using System.Collections.Concurrent;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Gelf.Extensions.Logging
{
    public class TcpGelfClient : IGelfClient
    {
        private static readonly byte[] Separator = { 0x00 };

        private readonly GelfLoggerOptions _options;

        private readonly ConcurrentBag<NetworkStream> _streams;

        public TcpGelfClient(GelfLoggerOptions options)
        {
            _options = options;
            _streams = new ConcurrentBag<NetworkStream>();
        }

        public async Task SendMessageAsync(GelfMessage message)
        {
            NetworkStream stream = null;
            try
            {
                stream = await GetStream();
                await message.WriteToStreamAsync(stream);
                await stream.WriteAsync(Separator, 0, Separator.Length);
            }
            catch (SocketException)
            {
                stream?.Dispose();
                stream = null;
            }
            finally
            {
                if (stream != null)
                    ReturnStream(stream);
            }
        }

        private async Task<NetworkStream> GetStream()
        {
            if (_streams.TryTake(out var stream))
                return stream;

            var client = new TcpClient();
            await client.ConnectAsync(_options.Host, _options.Port);
            return client.GetStream();
        }

        private void ReturnStream(NetworkStream stream)
        {
            _streams.Add(stream);
        }

        public void Dispose()
        {
            while (_streams.TryTake(out var socket))
            {
                socket.Dispose();
            }
        }
    }
}
