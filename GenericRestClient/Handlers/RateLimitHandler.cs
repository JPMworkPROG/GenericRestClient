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
      _logger.LogInformation("Applying rate limit in the request");
      await WaitForRateLimitAsync(cancellationToken);
      _logger.LogInformation("Rate limit applied in the request");

      return await base.SendAsync(request, cancellationToken);
   }

   private async Task WaitForRateLimitAsync(CancellationToken cancellationToken)
   {
      await _queueSemaphore.WaitAsync(cancellationToken);
      try
      {
         DateTime now = DateTime.UtcNow;
         DequeueOldRequests(now);

         if (_requestTimes.Count >= _options.RequestsPerMinute)
         {
            _logger.LogDebug(
               "Request limit reach ({CurrentCount}/{MaxRequests}), discarding request.",
               _requestTimes.Count,
               _options.RequestsPerMinute);
            throw new Exception("Rate limit reached");
         }
         else
         {
            _requestTimes.Enqueue(now);
            _logger.LogDebug(
               "New request registered, current count is: {CurrentCount}/{MaxRequests}",
               _requestTimes.Count,
               _options.RequestsPerMinute);
         }
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

   private void DequeueOldRequests(DateTime now)
   {
      int initialQueueSize = _requestTimes.Count;
      DateTime oneMinuteAgo = now.AddMinutes(-1);

      while (_requestTimes.Count > 0 && _requestTimes.Peek() < oneMinuteAgo)
      {
         _requestTimes.Dequeue();
      }

      int removedCount = initialQueueSize - _requestTimes.Count;
      if (removedCount > 0)
      {
         _logger.LogDebug(
            "Dequeued {RemovedCount} expired requests",
            removedCount);
      }
   }
}
