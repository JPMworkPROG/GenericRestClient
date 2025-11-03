using Microsoft.Extensions.Logging;

namespace GenericRestClient.Authentication;

internal sealed class NoAuthProvider : IAuthProvider
{
   private readonly ILogger<NoAuthProvider> _logger;

   public NoAuthProvider(ILogger<NoAuthProvider> logger)
   {
      _logger = logger;
      _logger.LogInformation("Authentication middleware 'NoAuthProvider' configured");
   }

   public Task<string> GetAccessTokenAsync(CancellationToken cancellationToken)
   {
      return Task.FromResult("NoAccessToken");
   }

   public Task SetAccessTokenAsync(HttpRequestMessage request, string accessToken, CancellationToken cancellationToken)
   {
      return Task.CompletedTask;
   }
}
