namespace GenericRestClient.Authentication;

internal sealed class NoAuthProvider : IAuthProvider
{
   public Task<string> GetAccessTokenAsync()
   {
      return Task.FromResult("NoAccessToken");
   }

   public Task SetAccessTokenAsync(HttpRequestMessage request, string accessToken, CancellationToken cancellationToken)
   {
      return Task.CompletedTask;
   }
}
