using GenericRestClient.Configuration;
using GenericRestClient.Core;
using GenericRestClient.Handlers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace GenericRestClient.Extensions;

public static class ServiceCollectionExtensions
{
   public static IServiceCollection AddGenericRestClient(
      this IServiceCollection services)
   {
      // Obter o LoggerFactory para criar um logger
      var serviceProvider = services.BuildServiceProvider();
      var loggerFactory = serviceProvider.GetService<ILoggerFactory>();
      var logger = loggerFactory?.CreateLogger("GenericRestClient.Extensions");

      logger?.LogInformation("Iniciando configuração da biblioteca GenericRestClient");

      try
      {
         // Registrar o RateLimitHandler
         logger?.LogDebug("Registrando RateLimitHandler como Transient");
         services.AddTransient<RateLimitHandler>();

         // Registrar HttpClient tipado para RestClient com handlers
         logger?.LogDebug("Registrando HttpClient tipado para RestClient com RateLimitHandler");
         services.AddHttpClient<IRestClient, RestClient>()
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
