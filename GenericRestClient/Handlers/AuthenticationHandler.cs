using GenericRestClient.Authentication;
using GenericRestClient.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GenericRestClient.Handlers;

public class AuthenticationHandler : DelegatingHandler
{
   private readonly IAuthProvider _authProvider;
   private readonly AuthenticationOptions _authOptions;
   private readonly ILogger<AuthenticationHandler> _logger;

   public AuthenticationHandler(
      IAuthProvider authProvider,
      IOptions<ApiClientOptions> options,
      ILogger<AuthenticationHandler> logger)
   {
      _authProvider = authProvider;
      _authOptions = options.Value.Authentication;
      _logger = logger;
   }

   protected override async Task<HttpResponseMessage> SendAsync(
      HttpRequestMessage request,
      CancellationToken cancellationToken)
   {
      if (!_authOptions.Enabled)
      {
         _logger.LogInformation($"Authentication middleware are disabled");
         return await base.SendAsync(request, cancellationToken);
      }

      _logger.LogInformation($"Appling credentials in the request");
      string accessToken = await _authProvider.GetAccessTokenAsync(cancellationToken);
      await _authProvider.SetAccessTokenAsync(request, accessToken, cancellationToken);
      _logger.LogInformation($"Credentials applied in the request");

      return await base.SendAsync(request, cancellationToken);
   }
}
