using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace GenericRestClient.Authentication;

internal sealed class NoAuthProvider : IAuthProvider
{
   public Task SetAccessTokenAsync(HttpRequestMessage request, CancellationToken cancellationToken)
   {
      return Task.CompletedTask;
   }
}
