using GenericRestClient.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;

namespace GenericRestClient.Authentication;

public class BearerTokenAuthProvider : IAuthProvider
{
   private readonly AuthenticationOptions _options;
   private readonly ILogger<BearerTokenAuthProvider> _logger;
   public BearerTokenAuthProvider(
      IOptions<ApiClientOptions> options,
      ILogger<BearerTokenAuthProvider> logger)
   {
      _options = options.Value.Authentication;
      _logger = logger;
   }

   public Task<string> GetAccessTokenAsync()
   {
      return Task.FromResult(_options.BearerToken!);
   }

   public Task SetAccessTokenAsync(HttpRequestMessage request, string accessToken, CancellationToken cancellationToken)
   {
      if (string.IsNullOrWhiteSpace(accessToken))
      {
         throw new InvalidOperationException("BearerToken must be provided for bearer authentication.");
      }

      request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
      _logger.LogDebug("Bearer token aplicado na requisição.");
      return Task.CompletedTask;
   }
}
