using System;
using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Gelf.Extensions.Logging
{
    public class TcpGelfClient : IGelfClient
    {
        private static readonly byte[] Separator = { 0x00 };

        private static readonly TimeSpan ExpirationTime = TimeSpan.FromMinutes(5);

        private readonly GelfLoggerOptions _options;

        private readonly ConcurrentQueue<Sender> _cachedSenders;

        private readonly Semaphore _semaphore;

        public TcpGelfClient(GelfLoggerOptions options)
        {
            _options = options;
            _cachedSenders = new ConcurrentQueue<Sender>();
            _semaphore = new Semaphore(options.MaxTcpConnections, options.MaxTcpConnections);
        }

        public async Task SendMessageAsync(GelfMessage message)
        {
            _semaphore.WaitOne();

            Sender sender = default;
            try
            {
                sender = await GetPooledSender();
                await message.WriteToStreamAsync(sender.Stream);
                await sender.Stream.WriteAsync(Separator, 0, Separator.Length);

                ReturnSender(sender);
            }
            catch (Exception)
            {
                DestroySender(sender);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        private async Task<Sender> GetPooledSender()
        {
            if (_cachedSenders.TryDequeue(out var sender))
                return sender;

            var client = new TcpClient();
            await client.ConnectAsync(_options.Host, _options.Port);
            return new Sender(client.GetStream());
        }

        private void DestroySender(Sender sender)
        {
            sender.Stream?.Dispose();
        }

        private void ReturnSender(Sender sender)
        {
            if (sender.Expire > DateTime.Now)
            {
                DestroySender(sender);
            }

            _cachedSenders.Enqueue(sender);
        }

        public void Dispose()
        {
            _semaphore.Dispose();
            while (_cachedSenders.TryDequeue(out var sender))
            {
                DestroySender(sender);
            }
        }

        internal struct Sender
        {
            public readonly NetworkStream Stream;

            public readonly DateTime Expire;

            public Sender(NetworkStream stream)
            {
                Stream = stream;
                Expire = DateTime.Now + ExpirationTime;
            }
        }
    }
}
