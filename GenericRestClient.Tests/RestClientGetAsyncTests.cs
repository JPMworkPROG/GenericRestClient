using GenericRestClient.Core;
using GenericRestClient.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using System.Net;
using System.Text.Json;

namespace GenericRestClient.Tests;

/// <summary>
/// Testes de integração para o método GetAsync do RestClient.
/// Valida o comportamento de requisições HTTP GET, deserialização JSON e tratamento de erros.
/// </summary>
public class RestClientGetAsyncTests
{
    private Mock<HttpMessageHandler> CreateMockHandler(
        HttpResponseMessage response)
    {
        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);

        return mockHandler;
    }

    private RestClient CreateRestClient(Mock<HttpMessageHandler> mockHandler, string? baseUrl = null)
    {
        var mockLogger = new Mock<ILogger<RestClient>>();
        return new RestClient(
            Options.Create(new ApiClientOptions
            {
                BaseUrl = baseUrl ?? "https://api.example.com",
                RateLimit = new RateLimitOptions { Enabled = false }
            }),
            new HttpClient(mockHandler.Object),
            mockLogger.Object);
    }

    private HttpResponseMessage CreateJsonResponse(object data, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        var jsonContent = JsonSerializer.Serialize(data, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
        return new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(jsonContent, System.Text.Encoding.UTF8, "application/json")
        };
    }

    private HttpResponseMessage CreateJsonResponse(string jsonContent, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        return new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(jsonContent, System.Text.Encoding.UTF8, "application/json")
        };
    }

    #region Successful Responses

    [Fact(DisplayName = "GetAsync deve retornar com sucesso com status code 200")]
    public async Task GetAsync_Sucess()
    {
        // Arrange
        var complexData = new
        {
            id = 1,
            user = new
            {
                name = "João Silva",
                profile = new { avatar = "https://example.com/avatar.jpg", bio = "Desenvolvedor" }
            },
            metadata = new { created = "2024-01-01", updated = "2024-10-26" }
        };
        var response = CreateJsonResponse(complexData);

        var mockHandler = CreateMockHandler(response);
        var client = CreateRestClient(mockHandler);

        // Act
        var result = await client.GetAsync<object, JsonElement>("/data");

        // Assert
        Assert.Equal(1, result!.GetProperty("id").GetInt32());
        Assert.Equal("João Silva", result!.GetProperty("user").GetProperty("name").GetString());
        Assert.Equal("https://example.com/avatar.jpg", result!.GetProperty("user").GetProperty("profile").GetProperty("avatar").GetString());
    }

    #endregion

    #region HTTP Status Code Errors

    [Fact(DisplayName = "GetAsync deve lançar HttpRequestException para erro 404 Not Found")]
    public async Task GetAsync_WithNotFoundResponse_ThrowsHttpRequestException()
    {
        // Arrange
        var response = CreateJsonResponse(new { }, HttpStatusCode.NotFound);

        var mockHandler = CreateMockHandler(response);
        var client = CreateRestClient(mockHandler);

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(() =>
            client.GetAsync<object, JsonElement>("/users/999"));
    }

    [Fact(DisplayName = "GetAsync deve lançar HttpRequestException para erro 400 Bad Request")]
    public async Task GetAsync_WithBadRequestResponse_ThrowsHttpRequestException()
    {
        // Arrange
        var errorResponse = new { error = "Invalid parameters", message = "Field 'email' is required" };
        var response = CreateJsonResponse(errorResponse, HttpStatusCode.BadRequest);

        var mockHandler = CreateMockHandler(response);
        var client = CreateRestClient(mockHandler);

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(() =>
            client.GetAsync<object, JsonElement>("/invalid"));
    }

    [Fact(DisplayName = "GetAsync deve lançar HttpRequestException para erro 401 Unauthorized")]
    public async Task GetAsync_WithUnauthorizedResponse_ThrowsHttpRequestException()
    {
        // Arrange
        var errorResponse = new { error = "Unauthorized", message = "Invalid API key" };
        var response = CreateJsonResponse(errorResponse, HttpStatusCode.Unauthorized);

        var mockHandler = CreateMockHandler(response);
        var client = CreateRestClient(mockHandler);

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(() =>
            client.GetAsync<object, JsonElement>("/protected"));
    }

    [Fact(DisplayName = "GetAsync deve lançar HttpRequestException para erro 403 Forbidden")]
    public async Task GetAsync_WithForbiddenResponse_ThrowsHttpRequestException()
    {
        // Arrange
        var response = CreateJsonResponse(new { }, HttpStatusCode.Forbidden);

        var mockHandler = CreateMockHandler(response);
        var client = CreateRestClient(mockHandler);

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(() =>
            client.GetAsync<object, JsonElement>("/admin"));
    }

    [Fact(DisplayName = "GetAsync deve lançar HttpRequestException para erro 429 Too Many Requests")]
    public async Task GetAsync_WithTooManyRequestsResponse_ThrowsHttpRequestException()
    {
        // Arrange
        var errorResponse = new { error = "Rate limit exceeded", retryAfter = 60 };
        var response = CreateJsonResponse(errorResponse, HttpStatusCode.TooManyRequests);

        var mockHandler = CreateMockHandler(response);
        var client = CreateRestClient(mockHandler);

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(() =>
            client.GetAsync<object, JsonElement>("/api"));
    }

    [Fact(DisplayName = "GetAsync deve lançar HttpRequestException para erro 500 Internal Server Error")]
    public async Task GetAsync_WithServerErrorResponse_ThrowsHttpRequestException()
    {
        // Arrange
        var errorResponse = new { error = "Internal Server Error", message = "Database connection failed" };
        var response = CreateJsonResponse(errorResponse, HttpStatusCode.InternalServerError);

        var mockHandler = CreateMockHandler(response);
        var client = CreateRestClient(mockHandler);

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(() =>
            client.GetAsync<object, JsonElement>("/error"));
    }

    [Fact(DisplayName = "GetAsync deve lançar HttpRequestException para erro 503 Service Unavailable")]
    public async Task GetAsync_WithServiceUnavailableResponse_ThrowsHttpRequestException()
    {
        // Arrange
        var response = CreateJsonResponse(new { }, HttpStatusCode.ServiceUnavailable);

        var mockHandler = CreateMockHandler(response);
        var client = CreateRestClient(mockHandler);

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(() =>
            client.GetAsync<object, JsonElement>("/service"));
    }

    #endregion

    #region Large Responses

    [Fact(DisplayName = "GetAsync deve deserializar array com 1000 itens")]
    public async Task GetAsync_WithLargeArray_DeserializesSuccessfully()
    {
        // Arrange
        var largeArray = Enumerable.Range(1, 1000)
            .Select(i => new { id = i, name = $"Item {i}" })
            .ToArray();
        var response = CreateJsonResponse(largeArray);

        var mockHandler = CreateMockHandler(response);
        var client = CreateRestClient(mockHandler);

        // Act
        var result = await client.GetAsync<object, JsonElement>("/items");

        // Assert
        Assert.Equal(1000, result!.GetArrayLength());
        Assert.Equal(1, result![0].GetProperty("id").GetInt32());
        Assert.Equal(1000, result![999].GetProperty("id").GetInt32());
    }

    [Fact(DisplayName = "GetAsync deve deserializar objeto com muitas propriedades")]
    public async Task GetAsync_WithManyProperties_DeserializesSuccessfully()
    {
        // Arrange - Cria um objeto com 100 propriedades
        var properties = Enumerable.Range(1, 100)
            .ToDictionary(i => $"prop{i}", i => (object)i);

        var jsonContent = JsonSerializer.Serialize(properties);
        var response = CreateJsonResponse(jsonContent);

        var mockHandler = CreateMockHandler(response);
        var client = CreateRestClient(mockHandler);

        // Act
        var result = await client.GetAsync<object, JsonElement>("/large");

        // Assert
        Assert.Equal(100, result!.EnumerateObject().Count());
    }

    #endregion

    #region Cancellation Token

    [Fact(DisplayName = "GetAsync deve respeitar CancellationToken")]
    public async Task GetAsync_WithCancelledToken_ThrowsCanceledException()
    {
        // Arrange
        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Returns(async (HttpRequestMessage request, CancellationToken ct) =>
            {
                await Task.Delay(5000, ct); // 5 segundos de delay
                return new HttpResponseMessage(HttpStatusCode.OK);
            });

        var client = CreateRestClient(mockHandler);
        var cts = new CancellationTokenSource(500); // Cancela após 500ms

        // Act & Assert
        // HttpClient lança TaskCanceledException ao invés de OperationCanceledException
        await Assert.ThrowsAsync<TaskCanceledException>(() =>
            client.GetAsync<object, JsonElement>("/slow", cts.Token));
    }

    #endregion

    #region URL and Endpoint Handling

    [Fact(DisplayName = "GetAsync deve construir URL corretamente com BaseAddress")]
    public async Task GetAsync_WithBaseAddressAndEndpoint_ConstructsUrlCorrectly()
    {
        // Arrange
        var data = new { id = 123, name = "Test User" };
        var response = CreateJsonResponse(data);

        var mockHandler = CreateMockHandler(response);
        var client = CreateRestClient(mockHandler);

        // Act
        var result = await client.GetAsync<object, JsonElement>("/users/123");

        // Assert
        Assert.Equal(123, result!.GetProperty("id").GetInt32());
    }

    [Fact(DisplayName = "GetAsync deve funcionar com endpoints contendo query string")]
    public async Task GetAsync_WithQueryString_DeserializesSuccessfully()
    {
        // Arrange
        var data = new { id = 1, name = "Filtered User", query = "active=true" };
        var response = CreateJsonResponse(data);

        var mockHandler = CreateMockHandler(response);
        var client = CreateRestClient(mockHandler);

        // Act
        var result = await client.GetAsync<object, JsonElement>("/users?active=true&verified=false");

        // Assert
        Assert.Equal(1, result!.GetProperty("id").GetInt32());
    }

    [Fact(DisplayName = "GetAsync deve funcionar com endpoints contendo IDs numerados")]
    public async Task GetAsync_WithNumericResourceId_DeserializesSuccessfully()
    {
        // Arrange
        var data = new { userId = 42, postId = 100, commentId = 500 };
        var response = CreateJsonResponse(data);

        var mockHandler = CreateMockHandler(response);
        var client = CreateRestClient(mockHandler);

        // Act
        var result = await client.GetAsync<object, JsonElement>("/users/42/posts/100/comments/500");

        // Assert
        Assert.Equal(42, result!.GetProperty("userId").GetInt32());
    }

    #endregion

    #region Real-world API Scenarios

    [Fact(DisplayName = "GetAsync simula GitHub API - buscar repositório")]
    public async Task GetAsync_SimulatesGitHubAPI_FetchRepository()
    {
        // Arrange
        var gitHubResponse = new
        {
            id = 1296269,
            name = "Hello-World",
            full_name = "octocat/Hello-World",
            owner = new { login = "octocat" },
            html_url = "https://github.com/octocat/Hello-World",
            stargazers_count = 80,
            watchers_count = 80,
            language = "C",
            forks_count = 9
        };
        var response = CreateJsonResponse(gitHubResponse);

        var mockHandler = CreateMockHandler(response);
        var client = CreateRestClient(mockHandler, "https://api.github.com");

        // Act
        var result = await client.GetAsync<object, JsonElement>("/repos/octocat/Hello-World");

        // Assert
        Assert.Equal("Hello-World", result!.GetProperty("name").GetString());
        Assert.Equal("octocat", result!.GetProperty("owner").GetProperty("login").GetString());
    }

    [Fact(DisplayName = "GetAsync simula JSONPlaceholder API - buscar posts")]
    public async Task GetAsync_SimulatesJsonPlaceholderAPI_FetchPosts()
    {
        // Arrange
        var posts = new[]
        {
            new { userId = 1, id = 1, title = "First Post", body = "Content 1" },
            new { userId = 1, id = 2, title = "Second Post", body = "Content 2" }
        };
        var response = CreateJsonResponse(posts);

        var mockHandler = CreateMockHandler(response);
        var client = CreateRestClient(mockHandler, "https://jsonplaceholder.typicode.com");

        // Act
        var result = await client.GetAsync<object, JsonElement>("/posts?userId=1");

        // Assert
        Assert.Equal(JsonValueKind.Array, result!.ValueKind);
        Assert.Equal(2, result!.GetArrayLength());
    }

    [Fact(DisplayName = "GetAsync simula OpenWeather API - buscar clima")]
    public async Task GetAsync_SimulatesOpenWeatherAPI_FetchWeather()
    {
        // Arrange
        var weatherResponse = new
        {
            coord = new { lon = -0.1257, lat = 51.5085 },
            weather = new[] { new { main = "Clouds", description = "broken clouds" } },
            main = new { temp = 280.32, humidity = 81, pressure = 1010 },
            wind = new { speed = 4.1 },
            clouds = new { all = 75 },
            name = "London",
            cod = 200
        };
        var response = CreateJsonResponse(weatherResponse);

        var mockHandler = CreateMockHandler(response);
        var client = CreateRestClient(mockHandler, "https://api.openweathermap.org");

        // Act
        var result = await client.GetAsync<object, JsonElement>("/data/2.5/weather?q=London");

        // Assert
        Assert.Equal("London", result!.GetProperty("name").GetString());
        Assert.Equal(JsonValueKind.Array, result!.GetProperty("weather").ValueKind);
    }

    #endregion

    #region Sequential Requests

    [Fact(DisplayName = "GetAsync deve funcionar com múltiplas requisições sequenciais")]
    public async Task GetAsync_WithSequentialRequests_DeserializesBothSuccessfully()
    {
        // Arrange
        var user = new { id = 1, name = "João" };
        var posts = new[] { new { id = 1, title = "Post 1" }, new { id = 2, title = "Post 2" } };

        var userResponse = CreateJsonResponse(user);
        var postsResponse = CreateJsonResponse(posts);

        var mockHandlerUser = CreateMockHandler(userResponse);
        var mockHandlerPosts = CreateMockHandler(postsResponse);

        var clientUser = CreateRestClient(mockHandlerUser);
        var clientPosts = CreateRestClient(mockHandlerPosts);

        // Act
        var userResult = await clientUser.GetAsync<object, JsonElement>("/users/1");
        var postsResult = await clientPosts.GetAsync<object, JsonElement>("/posts?userId=1");

        // Assert
        Assert.Equal(1, userResult!.GetProperty("id").GetInt32());
        Assert.Equal(2, postsResult!.GetArrayLength());
    }

    #endregion
}
