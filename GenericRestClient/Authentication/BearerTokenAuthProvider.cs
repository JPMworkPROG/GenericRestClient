using GenericRestClient.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

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

      _logger.LogInformation("Authentication handler 'BearerToken' configured");
   }

   public Task<string> GetAccessTokenAsync()
   {
      _logger.LogDebug("Retrieving bearer token");
      string bearerToken = _authOptions.BearerToken;
      _logger.LogDebug("Bearer token retrieved");

      return Task.FromResult(bearerToken);
   }
}
