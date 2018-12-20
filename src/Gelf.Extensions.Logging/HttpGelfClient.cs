using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace Gelf.Extensions.Logging
{
    public class HttpGelfClient : IGelfClient
    {
        private readonly HttpClient _httpClient;
        
        public HttpGelfClient(GelfLoggerOptions options)
        {
            var uriBuilder = new UriBuilder
            {
                Scheme = options.Protocol.ToString().ToLower(),
                Host = options.Host,
                Port = options.Port
            };

            _httpClient = new HttpClient
            {
                BaseAddress = uriBuilder.Uri,
                Timeout = options.HttpTimeout
            };

            if (options.HttpHeaders != null)
            {
                foreach (var header in options.HttpHeaders)
                {
                    _httpClient.DefaultRequestHeaders.Add(header.Key, header.Value);
                }
            }
        }

        public async Task SendMessageAsync(GelfMessage message)
        {
            using (var stream = message.ToJsonStream())
            {
                await SendMessageAsync(stream);
            }
        }

        private async Task SendMessageAsync(Stream stream)
        {
            using (var content = new StreamContent(stream))
            using (var request = new HttpRequestMessage(HttpMethod.Post, "gelf"))
            {
                content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                request.Content = content;

                using (var response = await _httpClient.SendAsync(request))
                {
                    response.EnsureSuccessStatusCode();
                }
            }
        }

        public void Dispose()
        {
            _httpClient.Dispose();
        }
    }
}
