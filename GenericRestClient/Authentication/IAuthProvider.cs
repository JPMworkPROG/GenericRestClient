namespace GenericRestClient.Authentication;

public interface IAuthProvider
{
   Task<string> GetAccessTokenAsync(CancellationToken cancellationToken);
   Task SetAccessTokenAsync(HttpRequestMessage request, string accessToken, CancellationToken cancellationToken);
}
