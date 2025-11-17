using GenericRestClient.Configuration;
using GenericRestClient.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace GenericRestClient.Extensions;

public static class ServiceCollectionExtensions
{
   public static IHttpClientBuilder ConfigureGenericRestClient(
      this IServiceCollection services,
      IConfiguration? configuration = null)
   {
      if (configuration != null)
      {
         services.Configure<ApiClientOptions>(
            configuration.GetSection(ApiClientOptions.SectionName));
      }

      services.TryAddSingleton<IValidateOptions<ApiClientOptions>, ApiClientOptionsValidator>();
      return services.AddHttpClient<IRestClient, RestClient>();
   }
}
