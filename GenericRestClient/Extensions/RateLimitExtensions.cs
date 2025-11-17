using GenericRestClient.Handlers;
using Microsoft.Extensions.DependencyInjection;

namespace GenericRestClient.Extensions;

public static class RateLimitExtensions
{
   public static IHttpClientBuilder AddRateLimit(this IHttpClientBuilder builder)
   {
      builder.Services.AddTransient<RateLimitHandler>();
      return builder.AddHttpMessageHandler<RateLimitHandler>();
   }
}

