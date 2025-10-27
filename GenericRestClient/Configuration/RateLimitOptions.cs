namespace GenericRestClient.Configuration;

public class RateLimitOptions
{
   public bool Enabled { get; set; }
   public int RequestsPerMinute { get; set; } = 3;
}
