using GenericRestClient.Core;
using GenericRestClient.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SampleApp.Examples;

var builder = Host.CreateApplicationBuilder(args);
builder.Configuration
   .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
   .AddEnvironmentVariables();

// Registrar RestClient com configuração flexível
var httpClientBuilder = builder.Services.ConfigureGenericRestClient(builder.Configuration);

// Adicionar headers customizados dinamicamente via código
// Exemplo: User-Agent necessário para GitHub API
httpClientBuilder.ConfigureHttpClient(client =>
{
   if (!client.DefaultRequestHeaders.Contains("User-Agent"))
   {
      client.DefaultRequestHeaders.Add("User-Agent", "GenericRestClient/1.0");
   }
});

// Configurar handlers baseado nas opções
var options = builder.Configuration.GetSection("ApiClient").Get<GenericRestClient.Configuration.ApiClientOptions>();

if (options?.Authentication?.Enabled == true)
{
   var authType = options.Authentication.Type?.Trim().ToUpperInvariant();
   switch (authType)
   {
      case "BEARER":
         httpClientBuilder.AddBearerAuthentication();
         break;
      case "APIKEY":
         httpClientBuilder.AddApiKeyAuthentication();
         break;
      case "OAUTH2":
         httpClientBuilder.AddOAuth2Authentication();
         break;
   }
}

if (options?.RateLimit?.Enabled == true)
{
   httpClientBuilder.AddRateLimit();
}

if (options?.Retry?.Enabled == true)
{
   httpClientBuilder.AddRetry();
}

var host = builder.Build();

// Obter instâncias necessárias
var client = host.Services.GetRequiredService<IRestClient>();
var loggerFactory = host.Services.GetRequiredService<ILoggerFactory>();
var logger = loggerFactory.CreateLogger<RestClientExamples>();
var examples = new RestClientExamples(client, logger);

// Executar exemplos
try
{
    await examples.RunAllExamplesAsync(CancellationToken.None);
}
catch (Exception ex)
{
    logger.LogError(ex, "Erro fatal durante execução dos exemplos");
    Environment.Exit(1);
}
