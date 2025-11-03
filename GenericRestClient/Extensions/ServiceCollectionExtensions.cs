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

      logger?.LogInformation("Iniciando configuração da biblioteca GenericRestClient");

      try
      {
         logger?.LogDebug("Registrando providers");
         services.TryAddSingleton<IValidateOptions<ApiClientOptions>, ApiClientOptionsValidator>();
         services.TryAddSingleton<BearerTokenAuthProvider>();
         services.TryAddSingleton<IAuthProvider>(sp =>
         {
            var optionsAccessor = sp.GetRequiredService<IOptions<ApiClientOptions>>();
            var options = optionsAccessor.Value;
            var authOptions = options.Authentication;

            if (!authOptions.Enabled)
            {
               return new NoAuthProvider();
            }

            return authOptions.Type switch
            {
               "Bearer" => sp.GetRequiredService<BearerTokenAuthProvider>(),
               _ => throw new InvalidOperationException($"Unsupported authentication type: {authOptions.Type}")
            };
         });
         services.AddTransient<AuthenticationHandler>();
         services.AddTransient<RateLimitHandler>();


         logger?.LogDebug("Registrando GenericRestClient como cliente http");
         services.AddHttpClient<IRestClient, RestClient>()
            .AddHttpMessageHandler<AuthenticationHandler>()
            .AddHttpMessageHandler<RateLimitHandler>();

         logger?.LogInformation("Configuração da biblioteca GenericRestClient concluída com sucesso");
         return services;
      }
      catch (Exception ex)
      {
         logger?.LogError(
            ex,
            "Erro ao configurar a biblioteca GenericRestClient: {ErrorMessage}",
            ex.Message);
         throw;
      }
   }
}
