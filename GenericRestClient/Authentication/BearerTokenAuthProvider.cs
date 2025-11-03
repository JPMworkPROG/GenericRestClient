using GenericRestClient.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;

namespace GenericRestClient.Authentication;

public class BearerTokenAuthProvider : IAuthProvider
{
   private readonly AuthenticationOptions _authOptions;
   private readonly ILogger<BearerTokenAuthProvider> _logger;
   public BearerTokenAuthProvider(
      IOptions<ApiClientOptions> options,
      ILogger<BearerTokenAuthProvider> logger)
   {
      _authOptions = options.Value.Authentication;
      _logger = logger;

      _logger.LogInformation("Authentication middleware 'BearerToken' configured");
   }

   public Task<string> GetAccessTokenAsync(CancellationToken cancellationToken)
   {
      _logger.LogDebug("Retrieving bearer token");
      string bearerToken = _authOptions.BearerToken;
      _logger.LogDebug("Bearer token retrieved");

      return Task.FromResult(bearerToken);
   }

   public Task SetAccessTokenAsync(HttpRequestMessage request, string accessToken, CancellationToken cancellationToken)
   {
      _logger.LogDebug("Assigning bearer token to request header");
      request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
      _logger.LogDebug("Bearer token assigned to header");
      return Task.CompletedTask;
   }
}
