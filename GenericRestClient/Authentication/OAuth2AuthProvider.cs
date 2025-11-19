using GenericRestClient.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GenericRestClient.Authentication;

public class OAuth2AuthProvider : IAuthProvider
{
   private readonly AuthenticationOptions _authOptions;
   private readonly IHttpClientFactory _httpClientFactory;
   private readonly ILogger<OAuth2AuthProvider> _logger;

   private readonly SemaphoreSlim _tokenSemaphore = new(1, 1);
   private string? _cachedAccessToken;
   private DateTimeOffset _tokenExpiresAt = DateTimeOffset.MinValue;

   public OAuth2AuthProvider(
      IOptions<ApiClientOptions> options,
      IHttpClientFactory httpClientFactory,
      ILogger<OAuth2AuthProvider> logger)
   {
      _authOptions = options.Value.Authentication;
      _httpClientFactory = httpClientFactory;
      _logger = logger;

      _logger.LogInformation("Authentication handler 'OAuth2' configured");
   }

   public async Task<string> GetAccessTokenAsync()
   {
      _logger.LogDebug("Retrieving OAuth2 access token");

      if (TokenIsValid())
      {
         _logger.LogDebug("OAuth2 access token retrieved from cache");
         return _cachedAccessToken!;
      }

      await _tokenSemaphore.WaitAsync();
      try
      {
         if (TokenIsValid())
         {
            _logger.LogDebug("OAuth2 access token retrieved from cache (post-semaphore)");
            return _cachedAccessToken!;
         }

         var tokenResponse = await RequestTokenAsync();

         _cachedAccessToken = tokenResponse.AccessToken;
         _tokenExpiresAt = CalculateTokenExpiration(tokenResponse.ExpiresIn);

         _logger.LogDebug(
            "OAuth2 access token acquired. Expires at {Expiration:o}",
            _tokenExpiresAt);

         return _cachedAccessToken;
      }
      finally
      {
         _tokenSemaphore.Release();
      }
   }

   private bool TokenIsValid()
   {
      return !string.IsNullOrWhiteSpace(_cachedAccessToken) &&
             DateTimeOffset.UtcNow < _tokenExpiresAt;
   }

   private DateTimeOffset CalculateTokenExpiration(int? expiresInSeconds)
   {
      if (expiresInSeconds is null or <= 0)
      {
         return DateTimeOffset.UtcNow.AddMinutes(5);
      }

      var refreshSkew = Math.Clamp(
         _authOptions.TokenRefreshSkewSeconds,
         0,
         expiresInSeconds.Value - 1);

      return DateTimeOffset.UtcNow.AddSeconds(expiresInSeconds.Value - refreshSkew);
   }

   private async Task<OAuthTokenResponse> RequestTokenAsync()
   {
      using var client = _httpClientFactory.CreateClient(nameof(OAuth2AuthProvider));

      var request = new HttpRequestMessage(HttpMethod.Post, _authOptions.TokenEndpoint)
      {
         Content = BuildTokenRequestContent()
      };

      request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

      var response = await client.SendAsync(request);

      if (!response.IsSuccessStatusCode)
      {
         var errorContent = await response.Content.ReadAsStringAsync();
         _logger.LogError(
            "OAuth2 token endpoint returned {StatusCode}. Response: {Response}",
            (int)response.StatusCode,
            errorContent);

         response.EnsureSuccessStatusCode();
      }

      await using var responseStream = await response.Content.ReadAsStreamAsync();
      var tokenResponse = await JsonSerializer.DeserializeAsync<OAuthTokenResponse>(
         responseStream);

      if (tokenResponse is null || string.IsNullOrWhiteSpace(tokenResponse.AccessToken))
      {
         throw new InvalidOperationException("OAuth2 token response did not contain an access_token.");
      }

      return tokenResponse;
   }

   private FormUrlEncodedContent BuildTokenRequestContent()
   {
      var parameters = new Dictionary<string, string>
      {
         ["grant_type"] = _authOptions.GrantType,
         ["client_id"] = _authOptions.ClientId,
         ["client_secret"] = _authOptions.ClientSecret
      };

      return new FormUrlEncodedContent(parameters);
   }

   private sealed record OAuthTokenResponse(
      [property: JsonPropertyName("access_token")] string AccessToken,
      [property: JsonPropertyName("token_type")] string? TokenType,
      [property: JsonPropertyName("expires_in")] int? ExpiresIn);
}
