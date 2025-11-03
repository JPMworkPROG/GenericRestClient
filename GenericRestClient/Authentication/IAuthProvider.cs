using System.Net.Http;
using System.Threading;

namespace GenericRestClient.Authentication;

public interface IAuthProvider
{
   Task SetAccessTokenAsync(HttpRequestMessage request, CancellationToken cancellationToken);
}
