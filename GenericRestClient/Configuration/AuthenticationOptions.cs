namespace GenericRestClient.Configuration;

public class AuthenticationOptions
{
   public bool Enabled { get; set; }
   public string? Type { get; set; }
   public string? BearerToken { get; set; }

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

      var normalizedType = Type.Trim();

      switch (normalizedType.ToLowerInvariant())
      {
         case "bearer":
            if (string.IsNullOrWhiteSpace(BearerToken))
            {
               throw new InvalidOperationException(
                  "BearerToken is required when Authentication Type is 'Bearer'.");
            }
            break;

         default:
            throw new InvalidOperationException(
               $"Unsupported authentication type '{Type}'. Supported types: Bearer.");
      }
   }
}
