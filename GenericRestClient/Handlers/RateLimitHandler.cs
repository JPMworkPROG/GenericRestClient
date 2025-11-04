using GenericRestClient.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Threading;

namespace GenericRestClient.Handlers;

public class RateLimitHandler : DelegatingHandler
{
   private readonly RateLimitOptions _options;
   private readonly Queue<DateTime> _requestTimes = new();
   private readonly SemaphoreSlim _queueSemaphore = new(1, 1);
   private readonly ILogger<RateLimitHandler> _logger;

   public RateLimitHandler(IOptions<ApiClientOptions> options, ILogger<RateLimitHandler> logger)
   {
      _options = options.Value.RateLimit;
      _logger = logger;

      _logger.LogInformation("Ratelimit middleware configured");
   }

   protected override async Task<HttpResponseMessage> SendAsync(
      HttpRequestMessage request,
      CancellationToken cancellationToken)
   {
      if (!_options.Enabled)
      {
         _logger.LogInformation($"Ratelimit middleware are disabled");
         return await base.SendAsync(request, cancellationToken);
      }

      _logger.LogInformation($"Appling rate limit in the request");
      await WaitForRateLimitAsync(cancellationToken);
      _logger.LogInformation($"Rate limit applied in the request");

      return await base.SendAsync(request, cancellationToken);
   }

   private async Task WaitForRateLimitAsync(CancellationToken cancellationToken)
   {
      TimeSpan delayRequired;

      // Verifica e calcula tempo de espera necessário
      await _queueSemaphore.WaitAsync(cancellationToken);
      try
      {
         var now = DateTime.UtcNow;
         var oneMinuteAgo = now.AddMinutes(-1);

         var initialQueueSize = _requestTimes.Count;

         // Remove requisições antigas (fora da janela de 1 minuto)
         while (_requestTimes.Count > 0 && _requestTimes.Peek() < oneMinuteAgo)
         {
            _requestTimes.Dequeue();
         }

         var removedCount = initialQueueSize - _requestTimes.Count;
         if (removedCount > 0)
         {
            _logger.LogDebug(
               "Removidas {RemovedCount} requisições expiradas da janela de 1 minuto",
               removedCount);
         }

         // Se já atingiu o limite de requisições no período, calcula o tempo de espera
         if (_requestTimes.Count >= _options.RequestsPerMinute)
         {
            var oldestRequest = _requestTimes.Peek();
            delayRequired = oldestRequest.AddMinutes(1) - now;

            _logger.LogWarning(
               "Limite de requisições atingido ({CurrentCount}/{MaxLimit}). Aguardando {WaitTimeMs}ms até a expiração da requisição mais antiga",
               _requestTimes.Count,
               _options.RequestsPerMinute,
               delayRequired.TotalMilliseconds);
         }
         else
         {
            delayRequired = TimeSpan.Zero;
         }
      }
      finally
      {
         _queueSemaphore.Release();
      }

      // Aguarda FORA da região crítica para não bloquear outras requisições
      if (delayRequired > TimeSpan.Zero)
      {
         try
         {
            _logger.LogDebug("Iniciando aguarde de rate limit por {DelayMs}ms", delayRequired.TotalMilliseconds);
            await Task.Delay(delayRequired, cancellationToken);
            _logger.LogDebug("Aguarde do rate limit concluído, prosseguindo com a requisição");
         }
         catch (OperationCanceledException)
         {
            _logger.LogWarning("Aguarde do rate limit foi cancelado");
            throw;
         }
      }

      // Registra a requisição APÓS aguardar
      await _queueSemaphore.WaitAsync(cancellationToken);
      try
      {
         // Remove requisições expiradas novamente (caso tenha esperado)
         var now = DateTime.UtcNow;
         var oneMinuteAgo = now.AddMinutes(-1);

         while (_requestTimes.Count > 0 && _requestTimes.Peek() < oneMinuteAgo)
         {
            _requestTimes.Dequeue();
         }

         // Registra a nova requisição
         _requestTimes.Enqueue(now);
         _logger.LogDebug(
            "Nova requisição registrada, contagem atual: {CurrentCount}/{MaxLimit}",
            _requestTimes.Count,
            _options.RequestsPerMinute);
      }
      finally
      {
         _queueSemaphore.Release();
      }
   }

   protected override void Dispose(bool disposing)
   {
      if (disposing)
      {
         _queueSemaphore.Dispose();
      }

      base.Dispose(disposing);
   }
}
