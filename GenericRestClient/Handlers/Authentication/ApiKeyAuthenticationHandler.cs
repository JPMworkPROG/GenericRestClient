using GenericRestClient.Authentication;
using GenericRestClient.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GenericRestClient.Handlers.Authentication;

public class ApiKeyAuthenticationHandler : DelegatingHandler, IAuthenticationHandler
{
   private readonly ApiKeyAuthProvider _authProvider;
   private readonly AuthenticationOptions _authOptions;
   private readonly ILogger<ApiKeyAuthenticationHandler> _logger;

   public ApiKeyAuthenticationHandler(
      ApiKeyAuthProvider authProvider,
      IOptions<ApiClientOptions> options,
      ILogger<ApiKeyAuthenticationHandler> logger)
   {
      _authProvider = authProvider;
      _authOptions = options.Value.Authentication;
      _logger = logger;
   }

   public string AuthenticationType => "ApiKey";

   protected override async Task<HttpResponseMessage> SendAsync(
      HttpRequestMessage request,
      CancellationToken cancellationToken)
   {
      _logger.LogInformation("Applying ApiKey credentials to the request");
      var token = await _authProvider.GetAccessTokenAsync();

      if (string.IsNullOrWhiteSpace(_authOptions.ApiKeyHeader))
      {
         _logger.LogDebug("Assigning API key to request URI");
         request.RequestUri = AppendQueryParameter(request.RequestUri, "apiKey", token);
         _logger.LogDebug("API key assigned to request URI");
      }
      else
      {
         _logger.LogDebug("Assigning API key to request header {Header}", _authOptions.ApiKeyHeader);
         request.Headers.Remove(_authOptions.ApiKeyHeader);
         request.Headers.TryAddWithoutValidation(_authOptions.ApiKeyHeader, token);
         _logger.LogDebug("API key assigned to header {Header}", _authOptions.ApiKeyHeader);
      }

      _logger.LogInformation("ApiKey credentials applied to the request");
      return await base.SendAsync(request, cancellationToken);
   }

   private static Uri AppendQueryParameter(Uri? requestUri, string key, string value)
   {
      if (requestUri is null)
      {
         throw new InvalidOperationException("RequestUri must be defined before applying API key authentication.");
      }

      var builder = new UriBuilder(requestUri);
      var query = builder.Query;

      if (!string.IsNullOrEmpty(query))
      {
         query = query.TrimStart('?') + "&";
      }

      query += $"{Uri.EscapeDataString(key)}={Uri.EscapeDataString(value)}";
      builder.Query = query;

      return builder.Uri;
   }
}

