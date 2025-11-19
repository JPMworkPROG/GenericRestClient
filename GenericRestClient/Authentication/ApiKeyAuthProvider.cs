using GenericRestClient.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GenericRestClient.Authentication;

public class ApiKeyAuthProvider : IAuthProvider
{
   private readonly AuthenticationOptions _authOptions;
   private readonly ILogger<ApiKeyAuthProvider> _logger;

   public ApiKeyAuthProvider(
      IOptions<ApiClientOptions> options,
      ILogger<ApiKeyAuthProvider> logger)
   {
      _authOptions = options.Value.Authentication;
      _logger = logger;

      _logger.LogInformation("Authentication handler 'ApiKey' configured");
   }

   public Task<string> GetAccessTokenAsync()
   {
      _logger.LogDebug("Retrieving api key");
      string apiKey = _authOptions.ApiKey;
      _logger.LogDebug("Api key retrieved");

      return Task.FromResult(apiKey);
   }
}