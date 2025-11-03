namespace GenericRestClient.Configuration;

public class AuthenticationOptions
{
   public bool Enabled { get; set; } = false;
   public string Type { get; set; } = String.Empty;
   public string BearerToken { get; set; } = String.Empty;

   public void Validate()
   {
      if (!Enabled)
      {
         return;
      }

      if (string.IsNullOrWhiteSpace(Type))
      {
         throw new InvalidOperationException("Authentication Type is required when authentication is enabled.");
      }

      switch (Type.Trim().ToLowerInvariant())
      {
         case "bearer":
            BearerValidation();
            break;

         default:
            throw new InvalidOperationException($"Unsupported authentication type '{Type}'. Supported types: Bearer.");
      }
   }

   public void BearerValidation()
   {
      if (string.IsNullOrWhiteSpace(BearerToken))
      {
         throw new InvalidOperationException(
            "BearerToken is required when Authentication Type is 'Bearer'.");
      }
   }
}
