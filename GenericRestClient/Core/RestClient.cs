using GenericRestClient.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http.Json;
using System.Text.Json;

namespace GenericRestClient.Core;

public class RestClient : IRestClient
{
   private readonly ApiClientOptions _options;
   private readonly HttpClient _httpClient;
   private readonly JsonSerializerOptions _jsonOptions;
   private readonly ILogger<RestClient> _logger;

   public RestClient(IOptions<ApiClientOptions> options, HttpClient httpClient, ILogger<RestClient> logger)
   {
      _options = options.Value;
      _httpClient = httpClient;
      _logger = logger;

      _httpClient.BaseAddress = new Uri(_options.BaseUrl);
      _jsonOptions = new JsonSerializerOptions
      {
         PropertyNameCaseInsensitive = true,
         PropertyNamingPolicy = JsonNamingPolicy.CamelCase
      };

      _logger.LogInformation(
         "RestClient iniciado com BaseUrl: {BaseUrl}",
         _options.BaseUrl);
      _logger.LogDebug(
         "Configurações JSON: PropertyNameCaseInsensitive={CaseInsensitive}, NamingPolicy=CamelCase",
         _jsonOptions.PropertyNameCaseInsensitive);
   }

   public async Task<TResponse?> GetAsync<TRequqest, TResponse>(string endpoint, CancellationToken cancellationToken = default)
   {
      _logger.LogInformation(
         "Iniciando requisição GET para o endpoint: {Endpoint}",
         endpoint);

      try
      {
         var requestUri = new Uri(_httpClient.BaseAddress!, endpoint);
         _logger.LogDebug(
            "URL completa da requisição: {FullUrl}",
            requestUri.AbsoluteUri);

         var startTime = DateTime.UtcNow;
         var response = await _httpClient.GetAsync(endpoint, cancellationToken);
         var duration = DateTime.UtcNow - startTime;

         _logger.LogDebug(
            "Requisição GET concluída para {Endpoint} com status {StatusCode} em {Duration}ms",
            endpoint,
            response.StatusCode,
            duration.TotalMilliseconds);

         var result = await DeserializeResponseAsync<TResponse>(response, cancellationToken);

         _logger.LogInformation(
            "Requisição GET processada com sucesso para {Endpoint}",
            endpoint);

         return result;
      }
      catch (HttpRequestException ex)
      {
         _logger.LogError(
            ex,
            "Erro ao executar requisição GET para {Endpoint}: {ErrorMessage}",
            endpoint,
            ex.Message);
         throw;
      }
      catch (TaskCanceledException ex)
      {
         _logger.LogWarning(
            ex,
            "Requisição GET para {Endpoint} foi cancelada ou expirou",
            endpoint);
         throw;
      }
      catch (Exception ex)
      {
         _logger.LogError(
            ex,
            "Erro inesperado ao processar requisição GET para {Endpoint}: {ErrorMessage}",
            endpoint,
            ex.Message);
         throw;
      }
   }

   private async Task<T?> DeserializeResponseAsync<T>(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
   {
      _logger.LogDebug(
         "Iniciando desserialização da resposta com status {StatusCode}",
         response.StatusCode);

      try
      {
         response.EnsureSuccessStatusCode();
         _logger.LogDebug(
            "Status de resposta verificado com sucesso: {StatusCode}",
            response.StatusCode);

         // Handle empty responses
         if (response.Content.Headers.ContentLength == 0)
         {
            _logger.LogDebug(
               "Resposta está vazia (ContentLength = 0), retornando valor padrão");
            return default;
         }

         var contentType = response.Content.Headers.ContentType?.MediaType;
         _logger.LogDebug(
            "Content-Type da resposta: {ContentType}",
            contentType ?? "não especificado");

         // Only deserialize JSON responses
         if (contentType != null && contentType.Contains("application/json", StringComparison.OrdinalIgnoreCase))
         {
            _logger.LogDebug(
               "Desserializando resposta JSON");
            return await response.Content.ReadFromJsonAsync<T>(_jsonOptions, cancellationToken);
         }

         _logger.LogWarning(
            "Content-Type não é JSON ({ContentType}), retornando valor padrão",
            contentType ?? "não especificado");
         return default;
      }
      catch (HttpRequestException ex)
      {
         _logger.LogError(
            ex,
            "Erro HTTP ao desserializar resposta: {ErrorMessage}",
            ex.Message);
         throw;
      }
      catch (JsonException ex)
      {
         _logger.LogError(
            ex,
            "Erro ao desserializar JSON: {ErrorMessage}",
            ex.Message);
         throw;
      }
      catch (Exception ex)
      {
         _logger.LogError(
            ex,
            "Erro inesperado ao desserializar resposta: {ErrorMessage}",
            ex.Message);
         throw;
      }
   }
}
