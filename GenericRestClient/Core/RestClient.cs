using GenericRestClient.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http.Json;
using System.Text.Json;

namespace GenericRestClient.Core;

public class RestClient : IRestClient
{
   private readonly ApiClientOptions _options;
   private readonly HttpClient _httpClient;
   private readonly JsonSerializerOptions _jsonOptions;
   private readonly ILogger<RestClient> _logger;

   public RestClient(IOptions<ApiClientOptions> options, HttpClient httpClient, ILogger<RestClient> logger)
   {
      _options = options.Value;
      _httpClient = httpClient;
      _logger = logger;

      _httpClient.BaseAddress = new Uri(_options.BaseUrl);
      _jsonOptions = new JsonSerializerOptions
      {
         PropertyNameCaseInsensitive = true,
         PropertyNamingPolicy = JsonNamingPolicy.CamelCase
      };

      _logger.LogInformation(
         "RestClient started with BaseUrl: {BaseUrl}",
         _options.BaseUrl);
      _logger.LogDebug(
         "JSON configuration: PropertyNameCaseInsensitive={CaseInsensitive}, NamingPolicy=CamelCase",
         _jsonOptions.PropertyNameCaseInsensitive);
   }

   public async Task<TResponse?> GetAsync<TResponse>(string endpoint, CancellationToken cancellationToken = default)
   {
      _logger.LogInformation(
         "Starting GET request to endpoint: {Endpoint}",
         endpoint);

      try
      {
         var requestUri = new Uri(_httpClient.BaseAddress!, endpoint);
         _logger.LogDebug(
            "Full request URL: {FullUrl}",
            requestUri.AbsoluteUri);

         var startTime = DateTime.UtcNow;
         var response = await _httpClient.GetAsync(endpoint, cancellationToken);
         var duration = DateTime.UtcNow - startTime;

         _logger.LogDebug(
            "GET request completed for {Endpoint} with status {StatusCode} in {Duration}ms",
            endpoint,
            response.StatusCode,
            duration.TotalMilliseconds);

         var result = await DeserializeResponseAsync<TResponse>(response, cancellationToken);

         _logger.LogInformation(
            "GET request processed successfully for {Endpoint}",
            endpoint);

         return result;
      }
      catch (HttpRequestException ex)
      {
         _logger.LogError(
            ex,
            "Error executing GET request to {Endpoint}: {ErrorMessage}",
            endpoint,
            ex.Message);
         throw;
      }
      catch (TaskCanceledException ex)
      {
         _logger.LogWarning(
            ex,
            "GET request to {Endpoint} was cancelled or expired",
            endpoint);
         throw;
      }
      catch (Exception ex)
      {
         _logger.LogError(
            ex,
            "Unexpected error processing GET request to {Endpoint}: {ErrorMessage}",
            endpoint,
            ex.Message);
         throw;
      }
   }

   public async Task<TResponse?> PostAsync<TRequest, TResponse>(string endpoint, TRequest body, CancellationToken cancellationToken = default)
   {
      _logger.LogInformation(
         "Starting POST request to endpoint: {Endpoint}",
         endpoint);

      try
      {
         var requestUri = new Uri(_httpClient.BaseAddress!, endpoint);
         _logger.LogDebug(
            "Full request URL: {FullUrl}",
            requestUri.AbsoluteUri);

         var startTime = DateTime.UtcNow;
         var response = await _httpClient.PostAsJsonAsync(endpoint, body, _jsonOptions, cancellationToken);
         var duration = DateTime.UtcNow - startTime;

         _logger.LogDebug(
            "POST request completed for {Endpoint} with status {StatusCode} in {Duration}ms",
            endpoint,
            response.StatusCode,
            duration.TotalMilliseconds);

         var result = await DeserializeResponseAsync<TResponse>(response, cancellationToken);

         _logger.LogInformation(
            "POST request processed successfully for {Endpoint}",
            endpoint);

         return result;
      }
      catch (HttpRequestException ex)
      {
         _logger.LogError(
            ex,
            "Error executing POST request to {Endpoint}: {ErrorMessage}",
            endpoint,
            ex.Message);
         throw;
      }
      catch (TaskCanceledException ex)
      {
         _logger.LogWarning(
            ex,
            "POST request to {Endpoint} was cancelled or expired",
            endpoint);
         throw;
      }
      catch (Exception ex)
      {
         _logger.LogError(
            ex,
            "Unexpected error processing POST request to {Endpoint}: {ErrorMessage}",
            endpoint,
            ex.Message);
         throw;
      }
   }

   public async Task<TResponse?> PutAsync<TRequest, TResponse>(string endpoint, TRequest body, CancellationToken cancellationToken = default)
   {
      _logger.LogInformation(
         "Starting PUT request to endpoint: {Endpoint}",
         endpoint);

      try
      {
         var requestUri = new Uri(_httpClient.BaseAddress!, endpoint);
         _logger.LogDebug(
            "Full request URL: {FullUrl}",
            requestUri.AbsoluteUri);

         var startTime = DateTime.UtcNow;
         var response = await _httpClient.PutAsJsonAsync(endpoint, body, _jsonOptions, cancellationToken);
         var duration = DateTime.UtcNow - startTime;

         _logger.LogDebug(
            "PUT request completed for {Endpoint} with status {StatusCode} in {Duration}ms",
            endpoint,
            response.StatusCode,
            duration.TotalMilliseconds);

         var result = await DeserializeResponseAsync<TResponse>(response, cancellationToken);

         _logger.LogInformation(
            "PUT request processed successfully for {Endpoint}",
            endpoint);

         return result;
      }
      catch (HttpRequestException ex)
      {
         _logger.LogError(
            ex,
            "Error executing PUT request to {Endpoint}: {ErrorMessage}",
            endpoint,
            ex.Message);
         throw;
      }
      catch (TaskCanceledException ex)
      {
         _logger.LogWarning(
            ex,
            "PUT request to {Endpoint} was cancelled or expired",
            endpoint);
         throw;
      }
      catch (Exception ex)
      {
         _logger.LogError(
            ex,
            "Unexpected error processing PUT request to {Endpoint}: {ErrorMessage}",
            endpoint,
            ex.Message);
         throw;
      }
   }

   public async Task DeleteAsync(string endpoint, CancellationToken cancellationToken = default)
   {
      _logger.LogInformation(
         "Starting DELETE request to endpoint: {Endpoint}",
         endpoint);

      try
      {
         var requestUri = new Uri(_httpClient.BaseAddress!, endpoint);
         _logger.LogDebug(
            "Full request URL: {FullUrl}",
            requestUri.AbsoluteUri);

         var startTime = DateTime.UtcNow;
         var response = await _httpClient.DeleteAsync(endpoint, cancellationToken);
         var duration = DateTime.UtcNow - startTime;

         _logger.LogDebug(
            "DELETE request completed for {Endpoint} with status {StatusCode} in {Duration}ms",
            endpoint,
            response.StatusCode,
            duration.TotalMilliseconds);

         response.EnsureSuccessStatusCode();

         _logger.LogInformation(
            "DELETE request processed successfully for {Endpoint}",
            endpoint);
      }
      catch (HttpRequestException ex)
      {
         _logger.LogError(
            ex,
            "Error executing DELETE request to {Endpoint}: {ErrorMessage}",
            endpoint,
            ex.Message);
         throw;
      }
      catch (TaskCanceledException ex)
      {
         _logger.LogWarning(
            ex,
            "DELETE request to {Endpoint} was cancelled or expired",
            endpoint);
         throw;
      }
      catch (Exception ex)
      {
         _logger.LogError(
            ex,
            "Unexpected error processing DELETE request to {Endpoint}: {ErrorMessage}",
            endpoint,
            ex.Message);
         throw;
      }
   }

   private async Task<T?> DeserializeResponseAsync<T>(
      HttpResponseMessage response,
      CancellationToken cancellationToken)
   {
      _logger.LogDebug(
         "Starting response deserialization with status {StatusCode}",
         response.StatusCode);

      try
      {
         response.EnsureSuccessStatusCode();
         _logger.LogDebug(
            "Response status verified successfully: {StatusCode}",
            response.StatusCode);

         // Handle empty responses
         if (response.Content.Headers.ContentLength == 0)
         {
            _logger.LogDebug(
               "Response is empty (ContentLength = 0), returning default value");
            return default;
         }

         var contentType = response.Content.Headers.ContentType?.MediaType;
         _logger.LogDebug(
            "Response Content-Type: {ContentType}",
            contentType ?? "not specified");

         // Only deserialize JSON responses
         if (contentType != null && contentType.Contains("application/json", StringComparison.OrdinalIgnoreCase))
         {
            _logger.LogDebug(
               "Deserializing JSON response");
            return await response.Content.ReadFromJsonAsync<T>(_jsonOptions, cancellationToken);
         }

         _logger.LogWarning(
            "Content-Type is not JSON ({ContentType}), returning default value",
            contentType ?? "not specified");
         return default;
      }
      catch (HttpRequestException ex)
      {
         _logger.LogError(
            ex,
            "HTTP error deserializing response: {ErrorMessage}",
            ex.Message);
         throw;
      }
      catch (JsonException ex)
      {
         _logger.LogError(
            ex,
            "Error deserializing JSON: {ErrorMessage}",
            ex.Message);
         throw;
      }
      catch (Exception ex)
      {
         _logger.LogError(
            ex,
            "Unexpected error deserializing response: {ErrorMessage}",
            ex.Message);
         throw;
      }
   }
}
