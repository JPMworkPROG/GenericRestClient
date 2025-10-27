using GenericRestClient.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GenericRestClient.Handlers;

public class RateLimitHandler : DelegatingHandler
{
   private readonly RateLimitOptions _options;
   private readonly Queue<DateTime> _requestTimes = new();
   private readonly SemaphoreSlim _queueLock = new(1, 1);
   private readonly ILogger<RateLimitHandler> _logger;

   public RateLimitHandler(IOptions<ApiClientOptions> options, ILogger<RateLimitHandler> logger)
   {
      _options = options.Value.RateLimit;
      _logger = logger;

      _logger.LogInformation(
         "RateLimitHandler inicializado - Habilitado: {Enabled}, Limite: {RequestsPerMinute} requisições por minuto",
         _options.Enabled,
         _options.RequestsPerMinute);
   }

   protected override async Task<HttpResponseMessage> SendAsync(
      HttpRequestMessage request,
      CancellationToken cancellationToken)
   {
      if (!_options.Enabled)
      {
         _logger.LogDebug(
            "Rate limit desabilitado, processando requisição {Method} {RequestUri} normalmente",
            request.Method,
            request.RequestUri);
         return await base.SendAsync(request, cancellationToken);
      }

      _logger.LogDebug(
         "Aplicando rate limit para requisição {Method} {RequestUri}",
         request.Method,
         request.RequestUri);

      await WaitForRateLimitAsync(cancellationToken);

      return await base.SendAsync(request, cancellationToken);
   }

   private async Task WaitForRateLimitAsync(CancellationToken cancellationToken)
   {
      await _queueLock.WaitAsync(cancellationToken);
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

         // Se já atingiu o limite de requisições no período, aguarda
         if (_requestTimes.Count >= _options.RequestsPerMinute)
         {
            var oldestRequest = _requestTimes.Peek();
            var timeUntilExpiry = oldestRequest.AddMinutes(1) - now;

            _logger.LogWarning(
               "Limite de requisições atingido ({CurrentCount}/{MaxLimit}). Aguardando {WaitTimeMs}ms até a expiração da requisição mais antiga",
               _requestTimes.Count,
               _options.RequestsPerMinute,
               timeUntilExpiry.TotalMilliseconds);

            if (timeUntilExpiry > TimeSpan.Zero)
            {
               try
               {
                  await Task.Delay(timeUntilExpiry, cancellationToken);
                  _logger.LogDebug(
                     "Aguarde do rate limit concluído, prosseguindo com a requisição");
               }
               catch (OperationCanceledException)
               {
                  _logger.LogWarning(
                     "Aguarde do rate limit foi cancelado");
                  throw;
               }
            }

            // Após aguardar, remove a requisição expirada
            _requestTimes.Dequeue();
            _logger.LogDebug(
               "Requisição expirada removida, nova contagem: {CurrentCount}/{MaxLimit}",
               _requestTimes.Count,
               _options.RequestsPerMinute);
         }

         // Registra a nova requisição
         _requestTimes.Enqueue(now);
         _logger.LogDebug(
            "Nova requisição registrada, contagem atual: {CurrentCount}/{MaxLimit}",
            _requestTimes.Count,
            _options.RequestsPerMinute);
      }
      catch (Exception ex)
      {
         _logger.LogError(
            ex,
            "Erro ao processar rate limit: {ErrorMessage}",
            ex.Message);
         throw;
      }
      finally
      {
         _queueLock.Release();
      }
   }

   protected override void Dispose(bool disposing)
   {
      if (disposing)
      {
         _logger.LogInformation(
            "RateLimitHandler sendo descartado, finalizando recursos");
         _queueLock.Dispose();
      }
      base.Dispose(disposing);
   }
}