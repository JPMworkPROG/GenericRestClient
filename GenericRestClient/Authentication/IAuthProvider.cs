namespace GenericRestClient.Authentication;

public interface IAuthProvider
{
   Task<string> GetAccessTokenAsync();
   Task SetAccessTokenAsync(HttpRequestMessage request, string accessToken, CancellationToken cancellationToken);
}
