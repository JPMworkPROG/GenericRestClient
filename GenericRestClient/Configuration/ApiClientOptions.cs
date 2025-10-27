namespace GenericRestClient.Configuration;

public class ApiClientOptions
{
   public const string SectionName = "ApiClient";
   public string BaseUrl { get; set; } = string.Empty;
   public RateLimitOptions RateLimit { get; set; } = new();
}