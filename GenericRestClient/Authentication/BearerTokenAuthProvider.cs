using GenericRestClient.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;

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

   public Task SetAccessTokenAsync(HttpRequestMessage request, CancellationToken cancellationToken)
   {
      var token = _options.BearerToken;

      if (string.IsNullOrWhiteSpace(token))
      {
         throw new InvalidOperationException("BearerToken must be provided for bearer authentication.");
      }

      request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
      _logger.LogDebug("Bearer token aplicado na requisição.");
      return Task.CompletedTask;
   }
}
