namespace GenericRestClient.Authentication;

public interface IAuthProvider
{
   Task<string> GetAccessTokenAsync();
}
