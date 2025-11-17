using GenericRestClient.Configuration;
using GenericRestClient.Handlers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GenericRestClient.Extensions;

public static class RetryExtensions
{
   public static IHttpClientBuilder AddRetry(this IHttpClientBuilder builder)
   {
      builder.Services.AddTransient<RetryHandler>();
      return builder.AddHttpMessageHandler<RetryHandler>();
   }
}

