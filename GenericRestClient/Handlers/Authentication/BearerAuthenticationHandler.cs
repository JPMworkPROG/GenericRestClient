using GenericRestClient.Authentication;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;

namespace GenericRestClient.Handlers.Authentication;

public class BearerAuthenticationHandler : DelegatingHandler
{
   private readonly BearerTokenAuthProvider _authProvider;
   private readonly ILogger<BearerAuthenticationHandler> _logger;

   public BearerAuthenticationHandler(
      BearerTokenAuthProvider authProvider,
      ILogger<BearerAuthenticationHandler> logger)
   {
      _authProvider = authProvider;
      _logger = logger;
   }

   protected override async Task<HttpResponseMessage> SendAsync(
      HttpRequestMessage request,
      CancellationToken cancellationToken)
   {
      _logger.LogInformation("Applying Bearer credentials to the request");
      var token = await _authProvider.GetAccessTokenAsync();
      request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
      _logger.LogInformation("Bearer credentials applied to the request");

      return await base.SendAsync(request, cancellationToken);
   }
}

