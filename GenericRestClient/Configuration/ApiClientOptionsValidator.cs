using GenericRestClient.Configuration;
using Microsoft.Extensions.Options;

namespace GenericRestClient.Configuration;

public class ApiClientOptionsValidator : IValidateOptions<ApiClientOptions>
{

   public ValidateOptionsResult Validate(string? name, ApiClientOptions options)
   {
      try
      {
         options.Validate();
         return ValidateOptionsResult.Success;
      }
      catch (Exception ex)
      {
         return ValidateOptionsResult.Fail($"ApiClient configuration validation failed: {ex.Message}");
      }
   }
}
