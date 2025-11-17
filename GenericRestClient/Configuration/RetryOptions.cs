namespace GenericRestClient.Configuration;

public class RetryOptions
{
   public bool Enabled { get; set; } = false;
   public int MaxRetries { get; set; } = 3;
   public int BaseDelayMilliseconds { get; set; } = 500;
   public bool UseExponentialBackoff { get; set; } = true;

   public void Validate()
   {
      if (!Enabled)
      {
         return;
      }

      if (MaxRetries < 0)
      {
         throw new InvalidOperationException("MaxRetries must be zero or a positive value.");
      }

      if (BaseDelayMilliseconds <= 0)
      {
         throw new InvalidOperationException("BaseDelayMilliseconds must be greater than 0.");
      }
   }
}

