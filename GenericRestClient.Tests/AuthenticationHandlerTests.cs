using GenericRestClient.Authentication;
using GenericRestClient.Configuration;
using GenericRestClient.Handlers.Authentication;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using System.Linq;
using System.Net;
using Xunit;

namespace GenericRestClient.Tests;

public class AuthenticationHandlerTests
{
   private static HttpResponseMessage CreateSuccessResponse() =>
      new(HttpStatusCode.OK)
      {
         Content = new StringContent("{\"success\": true}")
      };

   private Mock<HttpMessageHandler> CreateMockHandler(HttpResponseMessage response)
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

   private IOptions<ApiClientOptions> CreateOptions(
      bool enabled = true,
      string type = "Bearer",
      Action<AuthenticationOptions>? configure = null)
   {
      var authOptions = new AuthenticationOptions
      {
         Enabled = enabled,
         Type = type,
         ApiKey = "test-api-key-123",
         ApiKeyHeader = "X-API-Key",
         ClientId = "test-client-id",
         ClientSecret = "test-client-secret",
         TokenEndpoint = "https://auth.example.com/token",
         GrantType = "client_credentials",
         TokenRefreshSkewSeconds = 60
      };

      configure?.Invoke(authOptions);

      var apiClientOptions = new ApiClientOptions
      {
         BaseUrl = "https://api.example.com",
         Authentication = authOptions,
         RateLimit = new RateLimitOptions { Enabled = false }
      };

      return Options.Create(apiClientOptions);
   }

   private BearerAuthenticationHandler CreateBearerHandler(IOptions<ApiClientOptions> options)
   {
      var providerLogger = new Mock<ILogger<BearerTokenAuthProvider>>().Object;
      var handlerLogger = new Mock<ILogger<BearerAuthenticationHandler>>().Object;
      var provider = new BearerTokenAuthProvider(options, providerLogger);
      return new BearerAuthenticationHandler(provider, handlerLogger);
   }

   private ApiKeyAuthenticationHandler CreateApiKeyHandler(IOptions<ApiClientOptions> options)
   {
      var providerLogger = new Mock<ILogger<ApiKeyAuthProvider>>().Object;
      var handlerLogger = new Mock<ILogger<ApiKeyAuthenticationHandler>>().Object;
      var provider = new ApiKeyAuthProvider(options, providerLogger);
      return new ApiKeyAuthenticationHandler(provider, options, handlerLogger);
   }

   private OAuth2AuthenticationHandler CreateOAuth2Handler(
      IOptions<ApiClientOptions> options,
      IHttpClientFactory httpClientFactory)
   {
      var providerLogger = new Mock<ILogger<OAuth2AuthProvider>>().Object;
      var handlerLogger = new Mock<ILogger<OAuth2AuthenticationHandler>>().Object;
      var provider = new OAuth2AuthProvider(options, httpClientFactory, providerLogger);
      return new OAuth2AuthenticationHandler(provider, handlerLogger);
   }


   [Fact(DisplayName = "BearerAuthenticationHandler deve aplicar Authorization header")]
   public async Task BearerAuthenticationHandler_ShouldAddAuthorizationHeader()
   {
      // Arrange
      var options = CreateOptions(
         type: "Bearer",
         configure: auth => auth.ApiKey = "my-bearer-token-12345");

      var response = CreateSuccessResponse();
      var mockHandler = CreateMockHandler(response);
      var authHandler = CreateBearerHandler(options);
      authHandler.InnerHandler = mockHandler.Object;

      using var httpClient = new HttpClient(authHandler);

      // Act
      var request = new HttpRequestMessage(HttpMethod.Get, "https://api.example.com/test");
      var result = await httpClient.SendAsync(request);

      // Assert
      Assert.Equal(HttpStatusCode.OK, result.StatusCode);
      mockHandler.Protected().Verify(
         "SendAsync",
         Times.Once(),
         ItExpr.Is<HttpRequestMessage>(r =>
            r.Headers.Authorization != null &&
            r.Headers.Authorization.Scheme == "Bearer" &&
            r.Headers.Authorization.Parameter == "my-bearer-token-12345"),
         ItExpr.IsAny<CancellationToken>());
   }

   [Fact(DisplayName = "BearerTokenAuthProvider deve retornar token configurado")]
   public async Task BearerTokenAuthProvider_ShouldReturnConfiguredToken()
   {
      // Arrange
      var options = CreateOptions(
         type: "Bearer",
         configure: auth => auth.ApiKey = "test-bearer-token");

      var provider = new BearerTokenAuthProvider(
         options,
         new Mock<ILogger<BearerTokenAuthProvider>>().Object);

      // Act
      var token = await provider.GetAccessTokenAsync();

      // Assert
      Assert.Equal("test-bearer-token", token);
   }


   [Fact(DisplayName = "ApiKeyAuthenticationHandler deve adicionar header configurado")]
   public async Task ApiKeyAuthenticationHandler_WithHeader_ShouldAddConfiguredHeader()
   {
      // Arrange
      var options = CreateOptions(
         type: "ApiKey",
         configure: auth =>
         {
            auth.ApiKey = "my-api-key-12345";
            auth.ApiKeyHeader = "X-API-Key";
         });

      var response = CreateSuccessResponse();
      var mockHandler = CreateMockHandler(response);
      var authHandler = CreateApiKeyHandler(options);
      authHandler.InnerHandler = mockHandler.Object;

      using var httpClient = new HttpClient(authHandler);

      // Act
      var request = new HttpRequestMessage(HttpMethod.Get, "https://api.example.com/test");
      var result = await httpClient.SendAsync(request);

      // Assert
      Assert.Equal(HttpStatusCode.OK, result.StatusCode);
      mockHandler.Protected().Verify(
         "SendAsync",
         Times.Once(),
         ItExpr.Is<HttpRequestMessage>(r =>
            r.Headers.Contains("X-API-Key") &&
            r.Headers.GetValues("X-API-Key").Single() == "my-api-key-12345"),
         ItExpr.IsAny<CancellationToken>());
   }

   [Fact(DisplayName = "ApiKeyAuthenticationHandler deve adicionar query string quando header vazio")]
   public async Task ApiKeyAuthenticationHandler_WithQuery_ShouldAppendApiKeyParameter()
   {
      // Arrange
      var options = CreateOptions(
         type: "ApiKey",
         configure: auth =>
         {
            auth.ApiKey = "query-api-key";
            auth.ApiKeyHeader = string.Empty;
         });

      var response = CreateSuccessResponse();
      var mockHandler = CreateMockHandler(response);
      var authHandler = CreateApiKeyHandler(options);
      authHandler.InnerHandler = mockHandler.Object;

      using var httpClient = new HttpClient(authHandler);

      // Act
      var request = new HttpRequestMessage(HttpMethod.Get, "https://api.example.com/test?existing=1");
      var result = await httpClient.SendAsync(request);

      // Assert
      Assert.Equal(HttpStatusCode.OK, result.StatusCode);
      mockHandler.Protected().Verify(
         "SendAsync",
         Times.Once(),
         ItExpr.Is<HttpRequestMessage>(r =>
            r.RequestUri != null &&
            r.RequestUri.Query.Contains("existing=1") &&
            r.RequestUri.Query.Contains("apiKey=query-api-key")),
         ItExpr.IsAny<CancellationToken>());
   }

   [Fact(DisplayName = "ApiKeyAuthProvider deve retornar key configurada")]
   public async Task ApiKeyAuthProvider_ShouldReturnConfiguredApiKey()
   {
      // Arrange
      var options = CreateOptions(
         type: "ApiKey",
         configure: auth => auth.ApiKey = "configured-api-key");

      var provider = new ApiKeyAuthProvider(
         options,
         new Mock<ILogger<ApiKeyAuthProvider>>().Object);

      // Act
      var token = await provider.GetAccessTokenAsync();

      // Assert
      Assert.Equal("configured-api-key", token);
   }


   [Fact(DisplayName = "OAuth2AuthenticationHandler deve obter token e aplicar header")]
   public async Task OAuth2AuthenticationHandler_ShouldRequestTokenAndApplyHeader()
   {
      // Arrange
      var tokenResponse = new HttpResponseMessage(HttpStatusCode.OK)
      {
         Content = new StringContent(@"{
                ""access_token"": ""oauth2-access-token-12345"",
                ""token_type"": ""Bearer"",
                ""expires_in"": 3600
            }", System.Text.Encoding.UTF8, "application/json")
      };

      var apiResponse = CreateSuccessResponse();
      var tokenHandler = CreateMockHandler(tokenResponse);
      var apiHandler = CreateMockHandler(apiResponse);

      var options = CreateOptions(type: "OAuth2");

      var httpClientFactory = new Mock<IHttpClientFactory>();
      httpClientFactory
         .Setup(f => f.CreateClient(nameof(OAuth2AuthProvider)))
         .Returns(new HttpClient(tokenHandler.Object));

      var authHandler = CreateOAuth2Handler(options, httpClientFactory.Object);
      authHandler.InnerHandler = apiHandler.Object;

      using var httpClient = new HttpClient(authHandler);

      // Act
      var request = new HttpRequestMessage(HttpMethod.Get, "https://api.example.com/test");
      var result = await httpClient.SendAsync(request);

      // Assert
      Assert.Equal(HttpStatusCode.OK, result.StatusCode);

      tokenHandler.Protected().Verify(
         "SendAsync",
         Times.Once(),
         ItExpr.Is<HttpRequestMessage>(r =>
            r.Method == HttpMethod.Post &&
            r.RequestUri != null &&
            r.RequestUri.ToString() == "https://auth.example.com/token"),
         ItExpr.IsAny<CancellationToken>());

      apiHandler.Protected().Verify(
         "SendAsync",
         Times.Once(),
         ItExpr.Is<HttpRequestMessage>(r =>
            r.Headers.Authorization != null &&
            r.Headers.Authorization.Scheme == "Bearer" &&
            r.Headers.Authorization.Parameter == "oauth2-access-token-12345"),
         ItExpr.IsAny<CancellationToken>());
   }

   [Fact(DisplayName = "OAuth2AuthenticationHandler deve reutilizar token em cache")]
   public async Task OAuth2AuthenticationHandler_ShouldReuseCachedToken()
   {
      // Arrange
      var tokenResponse = new HttpResponseMessage(HttpStatusCode.OK)
      {
         Content = new StringContent(@"{
                ""access_token"": ""cached-token-12345"",
                ""token_type"": ""Bearer"",
                ""expires_in"": 3600
            }", System.Text.Encoding.UTF8, "application/json")
      };

      var apiResponse = CreateSuccessResponse();
      var tokenHandler = CreateMockHandler(tokenResponse);
      var apiHandler = CreateMockHandler(apiResponse);

      var options = CreateOptions(type: "OAuth2");

      var httpClientFactory = new Mock<IHttpClientFactory>();
      httpClientFactory
         .Setup(f => f.CreateClient(nameof(OAuth2AuthProvider)))
         .Returns(new HttpClient(tokenHandler.Object));

      var authHandler = CreateOAuth2Handler(options, httpClientFactory.Object);
      authHandler.InnerHandler = apiHandler.Object;

      using var httpClient = new HttpClient(authHandler);

      // Act
      var request1 = new HttpRequestMessage(HttpMethod.Get, "https://api.example.com/test1");
      var result1 = await httpClient.SendAsync(request1);

      var request2 = new HttpRequestMessage(HttpMethod.Get, "https://api.example.com/test2");
      var result2 = await httpClient.SendAsync(request2);

      // Assert
      Assert.Equal(HttpStatusCode.OK, result1.StatusCode);
      Assert.Equal(HttpStatusCode.OK, result2.StatusCode);

      tokenHandler.Protected().Verify(
         "SendAsync",
         Times.Once(),
         ItExpr.Is<HttpRequestMessage>(r => r.Method == HttpMethod.Post),
         ItExpr.IsAny<CancellationToken>());

      apiHandler.Protected().Verify(
         "SendAsync",
         Times.Exactly(2),
         ItExpr.IsAny<HttpRequestMessage>(),
         ItExpr.IsAny<CancellationToken>());
   }

   [Fact(DisplayName = "OAuth2AuthProvider deve lançar exceção quando endpoint retorna erro")]
   public async Task OAuth2AuthProvider_WhenEndpointFails_ShouldThrow()
   {
      // Arrange
      var tokenResponse = new HttpResponseMessage(HttpStatusCode.Unauthorized)
      {
         Content = new StringContent(@"{""error"": ""invalid_client""}")
      };

      var tokenHandler = CreateMockHandler(tokenResponse);
      var options = CreateOptions(type: "OAuth2");

      var httpClientFactory = new Mock<IHttpClientFactory>();
      httpClientFactory
         .Setup(f => f.CreateClient(nameof(OAuth2AuthProvider)))
         .Returns(new HttpClient(tokenHandler.Object));

      var provider = new OAuth2AuthProvider(
         options,
         httpClientFactory.Object,
         new Mock<ILogger<OAuth2AuthProvider>>().Object);

      // Act & Assert
      await Assert.ThrowsAsync<HttpRequestException>(() =>
         provider.GetAccessTokenAsync());
   }
}
