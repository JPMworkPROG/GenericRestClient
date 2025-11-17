using GenericRestClient.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Retry;
using System.Net;
using System.Net.Sockets;

namespace GenericRestClient.Handlers;

public class RetryHandler : DelegatingHandler
{
   private readonly RetryOptions _options;
   private readonly ILogger<RetryHandler> _logger;
   private readonly ResiliencePipeline<HttpResponseMessage> _retryPipeline;

   public RetryHandler(IOptions<ApiClientOptions> options, ILogger<RetryHandler> logger)
   {
      _options = options.Value.Retry;
      _logger = logger;
      _retryPipeline = BuildRetryPipeline();

      _logger.LogInformation(
         "Retry handler configured: MaxRetries={MaxRetries}, BaseDelay={BaseDelay}ms, ExponentialBackoff={ExponentialBackoff}",
         _options.MaxRetries,
         _options.BaseDelayMilliseconds,
         _options.UseExponentialBackoff);
   }

   protected override async Task<HttpResponseMessage> SendAsync(
      HttpRequestMessage request,
      CancellationToken cancellationToken)
   {
      var context = ResilienceContextPool.Shared.Get(cancellationToken);
      context.Properties.Set(new ResiliencePropertyKey<string>("RequestUri"), request.RequestUri?.ToString() ?? string.Empty);

      try
      {
         return await _retryPipeline.ExecuteAsync(
            async (ctx) =>
            {
               var response = await base.SendAsync(request, ctx.CancellationToken);
               return response;
            },
            context);
      }
      finally
      {
         ResilienceContextPool.Shared.Return(context);
      }
   }

    protected override HttpResponseMessage Send(
      HttpRequestMessage request,
      CancellationToken cancellationToken)
   {
      var context = ResilienceContextPool.Shared.Get(cancellationToken);
      context.Properties.Set(new ResiliencePropertyKey<string>("RequestUri"), request.RequestUri?.ToString() ?? string.Empty);

      try
      {
         return _retryPipeline.Execute(
            (ctx) =>
            {
               var response = base.Send(request, ctx.CancellationToken);
               return response;
            },
            context);
      }
      finally
      {
         ResilienceContextPool.Shared.Return(context);
      }
   }

   private ResiliencePipeline<HttpResponseMessage> BuildRetryPipeline()
   {
      var pipelineBuilder = new ResiliencePipelineBuilder<HttpResponseMessage>();

      pipelineBuilder.AddRetry(new Polly.Retry.RetryStrategyOptions<HttpResponseMessage>
      {
         ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
            .Handle<HttpRequestException>()
            .Handle<TaskCanceledException>()
            .Handle<SocketException>()
            .Handle<TimeoutException>()
            .HandleResult(response => ShouldRetryResponse(response)),
         MaxRetryAttempts = _options.MaxRetries,
         DelayGenerator = args =>
         {
            var delay = CalculateDelay(args.AttemptNumber, args.Outcome.Result);
            return new ValueTask<TimeSpan?>(delay);
         },
         OnRetry = args =>
         {
            var delay = args.Duration;
            var attempt = args.AttemptNumber;
            var exception = args.Outcome.Exception;
            var result = args.Outcome.Result;
            var uri = args.Context.Properties.GetValue(
               new ResiliencePropertyKey<string>("RequestUri"),
               "unknown");

            if (exception != null)
            {
               _logger.LogWarning(
                  exception,
                  "Attempt {Attempt}/{MaxRetries} failed for {Uri} with exception: {ExceptionType}. Waiting {Delay}ms before retrying",
                  attempt,
                  _options.MaxRetries + 1,
                  uri,
                  exception.GetType().Name,
                  delay.TotalMilliseconds);
            }
            else if (result != null)
            {
               var retryAfter = GetRetryAfterHeader(result);

               if (retryAfter.HasValue)
               {
                  _logger.LogWarning(
                     "Attempt {Attempt}/{MaxRetries} failed for {Uri} with status {StatusCode}. Waiting {Delay}ms before retrying (Retry-After: {RetryAfterSeconds}s)",
                     attempt,
                     _options.MaxRetries + 1,
                     uri,
                     result.StatusCode,
                     delay.TotalMilliseconds,
                     retryAfter.Value.TotalSeconds);
               }
               else
               {
                  _logger.LogWarning(
                     "Attempt {Attempt}/{MaxRetries} failed for {Uri} with status {StatusCode}. Waiting {Delay}ms before retrying",
                     attempt,
                     _options.MaxRetries + 1,
                     uri,
                     result.StatusCode,
                     delay.TotalMilliseconds);
               }
            }

            return default;
         }
      });

      return pipelineBuilder.Build();
   }

   private TimeSpan CalculateDelay(int attemptNumber, HttpResponseMessage? response)
   {
      var retryAfter = GetRetryAfterHeader(response);
      if (retryAfter.HasValue)
      {
         _logger.LogDebug(
            "Using delay from Retry-After header: {RetryAfter}ms",
            retryAfter.Value.TotalMilliseconds);
         return retryAfter.Value;
      }

      var baseDelay = TimeSpan.FromMilliseconds(_options.BaseDelayMilliseconds);
      TimeSpan calculatedDelay;
      
      if (_options.UseExponentialBackoff)
      {
         calculatedDelay = TimeSpan.FromMilliseconds(
            baseDelay.TotalMilliseconds * Math.Pow(2, attemptNumber - 1));
      }
      else
      {
         calculatedDelay = TimeSpan.FromMilliseconds(baseDelay.TotalMilliseconds * attemptNumber);
      }

      return calculatedDelay;
   }

   private static TimeSpan? GetRetryAfterHeader(HttpResponseMessage? response)
   {
      if (response?.Headers?.RetryAfter == null)
      {
         return null;
      }

      var retryAfter = response.Headers.RetryAfter;
      
      if (retryAfter.Delta.HasValue)
      {
         return retryAfter.Delta.Value;
      }

      if (retryAfter.Date.HasValue)
      {
         var delay = retryAfter.Date.Value - DateTimeOffset.UtcNow;
         return delay > TimeSpan.Zero ? delay : null;
      }

      return null;
   }

   private static bool ShouldRetryResponse(HttpResponseMessage response)
   {
      if (response == null)
      {
         return false;
      }

      var statusCode = (int)response.StatusCode;

      return statusCode == 429 || (statusCode >= 500 && statusCode < 600);
   }
}

