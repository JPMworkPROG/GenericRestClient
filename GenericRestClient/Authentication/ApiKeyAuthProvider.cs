namespace GenericRestClient.Authentication;

public class ApiKeyAuthProvider : IAuthProvider
{
   private readonly AuthenticationOptions _authOptions;
   private readonly ILogger<BearerTokenAuthProvider> _logger;
   public ApiKeyAuthProvider(
      IOptions<ApiClientOptions> options,
      ILogger<ApiKeyAuthProvider> logger)
   {
      _authOptions = options.Value.Authentication;
      _logger = logger;

      _logger.LogInformation("Authentication middleware 'ApiKey' configured");
   }

   public Task<string> GetAccessTokenAsync(CancellationToken cancellationToken)
   {
      _logger.LogDebug("Retrieving api key");
      string apiKey = _authOptions.ApiKey;
      _logger.LogDebug("Api key retrieved");

      return Task.FromResult(apiKey);
   }

   public Task SetAccessTokenAsync(HttpRequestMessage request, string accessToken, CancellationToken cancellationToken)
   {
      if (string.IsNullOrWhiteSpace(_authOptions.ApiKeyHeader))
      {
         _logger.LogDebug("Assigning api key to request URI");
         request.RequestUri = new Uri($"{request.RequestUri.AbsoluteUri}?apiKey={accessToken}");
         _logger.LogDebug("Api key assigned to request URI");

         return Task.CompletedTask;
      }

      _logger.LogDebug("Assigning api key to request header");
      request.Headers.Add(_authOptions.ApiKeyHeader, accessToken);
      _logger.LogDebug("Api key assigned to header");
      
      return Task.CompletedTask;
   }
}