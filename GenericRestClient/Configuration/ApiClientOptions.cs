namespace GenericRestClient.Configuration;

public class ApiClientOptions
{
   public const string SectionName = "ApiClient";
   public string BaseUrl { get; set; } = string.Empty;
   public RateLimitOptions RateLimit { get; set; } = new();
   public AuthenticationOptions Authentication { get; set; } = new();

   public void Validate()
   {
      if (string.IsNullOrWhiteSpace(BaseUrl))
      {
         throw new InvalidOperationException("BaseUrl is required.");
      }

      if (!Uri.TryCreate(BaseUrl, UriKind.Absolute, out _))
      {
         throw new InvalidOperationException($"BaseUrl '{BaseUrl}' is not a valid absolute URI.");
      }

      Authentication.Validate();
      RateLimit.Validate();
   }
}
