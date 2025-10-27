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
      this IServiceCollection services,
      IConfiguration configuration)
   {
      // Obter o LoggerFactory para criar um logger
      var serviceProvider = services.BuildServiceProvider();
      var loggerFactory = serviceProvider.GetService<ILoggerFactory>();
      var logger = loggerFactory?.CreateLogger("GenericRestClient.Extensions");

      logger?.LogInformation(
         "Iniciando configuração da biblioteca GenericRestClient");

      try
      {
         // Configurar opções
         logger?.LogDebug(
            "Carregando configurações de ApiClientOptions da seção '{SectionName}'",
            ApiClientOptions.SectionName);

         services.Configure<ApiClientOptions>(configuration.GetSection(ApiClientOptions.SectionName));

         // Validar configuração
         var apiClientSection = configuration.GetSection(ApiClientOptions.SectionName);
         if (!apiClientSection.Exists())
         {
            logger?.LogWarning(
               "Seção '{SectionName}' não encontrada na configuração. Usando valores padrão",
               ApiClientOptions.SectionName);
         }
         else
         {
            var baseUrl = apiClientSection["BaseUrl"];
            var rateLimitEnabled = apiClientSection["RateLimit:Enabled"];
            var requestsPerMinute = apiClientSection["RateLimit:RequestsPerMinute"];

            logger?.LogInformation(
               "Configurações carregadas - BaseUrl: {BaseUrl}, RateLimit Habilitado: {RateLimitEnabled}, Limite: {RequestsPerMinute} req/min",
               baseUrl,
               rateLimitEnabled,
               requestsPerMinute);
         }

         // Registrar o RateLimitHandler
         logger?.LogDebug(
            "Registrando RateLimitHandler como Transient");
         services.AddTransient<RateLimitHandler>();

         // Registrar HttpClient tipado para RestClient com handlers
         logger?.LogDebug(
            "Registrando HttpClient tipado para RestClient com RateLimitHandler");

         services.AddHttpClient<IRestClient, RestClient>()
            .AddHttpMessageHandler<RateLimitHandler>();

         logger?.LogInformation(
            "Configuração da biblioteca GenericRestClient concluída com sucesso");

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
