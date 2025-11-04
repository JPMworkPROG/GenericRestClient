namespace GenericRestClient.Configuration;

public class AuthenticationOptions
{
   public bool Enabled { get; set; } = false;
   public string Type { get; set; } = string.Empty;
   public string ApiKey { get; set; } = string.Empty;
   public string ClientId { get; set; } = string.Empty;
   public string ClientSecret { get; set; } = string.Empty;
   public string TokenEndpoint { get; set; } = string.Empty;
   public string GrantType { get; set; } = string.Empty;
   public int TokenRefreshSkewSeconds { get; set; } = 60;

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

         case "oauth2":
            OAuth2Validation();
            break;

         default:
            throw new InvalidOperationException($"Unsupported authentication type '{Type}'. Supported types: Bearer, OAuth2.");
      }
   }

   public void BearerValidation()
   {
      if (string.IsNullOrWhiteSpace(ApiKey))
      {
         throw new InvalidOperationException(
            "BearerToken is required when Authentication Type is 'Bearer'.");
      }
   }

   public void OAuth2Validation()
   {
      if (string.IsNullOrWhiteSpace(GrantType))
      {
         throw new InvalidOperationException(
            "GrantType is required when Authentication Type is 'OAuth2'.");
      }
      if (string.IsNullOrWhiteSpace(ClientId))
      {
         throw new InvalidOperationException(
            "ClientId is required when Authentication Type is 'OAuth2'.");
      }

      if (string.IsNullOrWhiteSpace(ClientSecret))
      {
         throw new InvalidOperationException(
            "ClientSecret is required when Authentication Type is 'OAuth2'.");
      }

      if (string.IsNullOrWhiteSpace(TokenEndpoint))
      {
         throw new InvalidOperationException(
            "TokenEndpoint is required when Authentication Type is 'OAuth2'.");
      }

      if (!Uri.TryCreate(TokenEndpoint, UriKind.Absolute, out var tokenEndpointUri))
      {
         throw new InvalidOperationException(
            $"TokenEndpoint '{TokenEndpoint}' is not a valid absolute URI.");
      }

      if (TokenRefreshSkewSeconds < 0)
      {
         throw new InvalidOperationException(
            "TokenRefreshSkewSeconds must be zero or a positive value.");
      }
   }
}
