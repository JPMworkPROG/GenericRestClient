using GenericRestClient.Authentication;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;

namespace GenericRestClient.Handlers.Authentication;

public class OAuth2AuthenticationHandler : DelegatingHandler, IAuthenticationHandler
{
   private readonly OAuth2AuthProvider _authProvider;
   private readonly ILogger<OAuth2AuthenticationHandler> _logger;

   public OAuth2AuthenticationHandler(
      OAuth2AuthProvider authProvider,
      ILogger<OAuth2AuthenticationHandler> logger)
   {
      _authProvider = authProvider;
      _logger = logger;
   }

   public string AuthenticationType => "OAuth2";

   protected override async Task<HttpResponseMessage> SendAsync(
      HttpRequestMessage request,
      CancellationToken cancellationToken)
   {
      _logger.LogInformation("Applying OAuth2 credentials to the request");
      var token = await _authProvider.GetAccessTokenAsync();
      request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
      _logger.LogInformation("OAuth2 credentials applied to the request");

      return await base.SendAsync(request, cancellationToken);
   }
}

