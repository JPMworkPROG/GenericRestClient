using GenericRestClient.Authentication;
using GenericRestClient.Configuration;
using GenericRestClient.Core;
using GenericRestClient.Handlers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace GenericRestClient.Extensions;

public static class ServiceCollectionExtensions
{
   public static IServiceCollection AddGenericRestClient(
      this IServiceCollection services)
   {
      var serviceProvider = services.BuildServiceProvider();
      var loggerFactory = serviceProvider.GetService<ILoggerFactory>();
      var logger = loggerFactory?.CreateLogger("GenericRestClient.Extensions");

      logger?.LogInformation("Starting configuration of the GenericRestClient library");

      try
      {
         logger?.LogDebug("Registering auth providers");
         services.TryAddSingleton<IValidateOptions<ApiClientOptions>, ApiClientOptionsValidator>();
         services.TryAddSingleton<BearerTokenAuthProvider>();
         services.TryAddSingleton<OAuth2AuthProvider>();
         services.TryAddSingleton<NoAuthProvider>();
         services.TryAddSingleton<IAuthProvider>(sp =>
         {
            var optionsAccessor = sp.GetRequiredService<IOptions<ApiClientOptions>>();
            var options = optionsAccessor.Value;
            var authOptions = options.Authentication;

            if (!authOptions.Enabled)
            {
               return sp.GetRequiredService<NoAuthProvider>();
            }

            return authOptions.Type switch
            {
               "Bearer" => sp.GetRequiredService<BearerTokenAuthProvider>(),
               "OAuth2" => sp.GetRequiredService<OAuth2AuthProvider>(),
               _ => throw new InvalidOperationException($"Unsupported authentication type: {authOptions.Type}")
            };
         });

         logger?.LogDebug("Registering request handlers");
         services.AddTransient<AuthenticationHandler>();
         services.AddTransient<RateLimitHandler>();


         logger?.LogDebug("Registering generic http client");
         services.AddHttpClient<IRestClient, RestClient>()
            .AddHttpMessageHandler<AuthenticationHandler>()
            .AddHttpMessageHandler<RateLimitHandler>();

         logger?.LogInformation("Success in configuration of the GenericRestClient library");
         return services;
      }
      catch (Exception ex)
      {
         logger?.LogError(
            ex,
            $"Error in configuration of the GenericRestClient {ex.Message}");
         throw;
      }
   }
}
