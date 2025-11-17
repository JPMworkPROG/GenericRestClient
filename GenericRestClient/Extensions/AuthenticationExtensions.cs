using GenericRestClient.Authentication;
using GenericRestClient.Handlers.Authentication;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace GenericRestClient.Extensions;

public static class AuthenticationExtensions
{
   public static IHttpClientBuilder AddBearerAuthentication(this IHttpClientBuilder builder)
   {
      builder.Services.TryAddSingleton<BearerTokenAuthProvider>();
      builder.Services.AddTransient<BearerAuthenticationHandler>();
      return builder.AddHttpMessageHandler<BearerAuthenticationHandler>();
   }

   public static IHttpClientBuilder AddApiKeyAuthentication(this IHttpClientBuilder builder)
   {
      builder.Services.TryAddSingleton<ApiKeyAuthProvider>();
      builder.Services.AddTransient<ApiKeyAuthenticationHandler>();
      return builder.AddHttpMessageHandler<ApiKeyAuthenticationHandler>();
   }

   public static IHttpClientBuilder AddOAuth2Authentication(this IHttpClientBuilder builder)
   {
      builder.Services.TryAddSingleton<OAuth2AuthProvider>();
      builder.Services.AddTransient<OAuth2AuthenticationHandler>();
      return builder.AddHttpMessageHandler<OAuth2AuthenticationHandler>();
   }
}

