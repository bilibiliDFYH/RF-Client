using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace Ra2Client.Domain.Multiplayer.CnCNet
{
    /// <summary>
    /// A web client that supports customizing the timeout of the request.
    /// </summary>
    class ExtendedHttpClient
    {
        private readonly HttpClient httpClient;

        public ExtendedHttpClient(int timeout)
        {
            var handler = new HttpClientHandler();
            httpClient = new HttpClient(handler)
            {
                Timeout = TimeSpan.FromMilliseconds(timeout)
            };
        }

        public async Task<string> GetStringAsync(Uri address)
        {
            return await httpClient.GetStringAsync(address);
        }
    }
}
