using GenericRestClient.Authentication;
using GenericRestClient.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GenericRestClient.Handlers;

public class AuthenticationHandler : DelegatingHandler
{
   private readonly IAuthProvider _authProvider;
   private readonly AuthenticationOptions _options;
   private readonly ILogger<AuthenticationHandler> _logger;

   public AuthenticationHandler(
      IAuthProvider authProvider,
      IOptions<ApiClientOptions> options,
      ILogger<AuthenticationHandler> logger)
   {
      _authProvider = authProvider;
      _options = options.Value.Authentication;
      _logger = logger;
   }

   protected override async Task<HttpResponseMessage> SendAsync(
      HttpRequestMessage request,
      CancellationToken cancellationToken)
   {
      if (!_options.Enabled)
      {
         _logger.LogDebug(
           "Autenticação desabilitada, processando requisição {Method} {RequestUri} normalmente",
           request.Method,
           request.RequestUri);
         return await base.SendAsync(request, cancellationToken);
      }

      string accessToken = await _authProvider.GetAccessTokenAsync();
      await _authProvider.SetAccessTokenAsync(request, accessToken, cancellationToken);
      _logger.LogInformation("Credenciais aplicadas pelo provider {Provider}", _authProvider.GetType().Name);

      return await base.SendAsync(request, cancellationToken);
   }
}
