namespace GenericRestClient.Configuration;

public class RateLimitOptions
{
   public bool Enabled { get; set; } = false;
   public int RequestsPerMinute { get; set; } = 3;

   public void Validate()
   {

      if (!Enabled)
      {
         return;
      }

      if (Enabled && RequestsPerMinute <= 0)
      {
         throw new InvalidOperationException("RequestsPerMinute must be greater than 0.");
      }
   }
}
