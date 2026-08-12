using System.Net;
using System.Net.Sockets;
using System.Text;
using FlexRadioServices.Events;

namespace FlexRadioServices.Models.Ports.Network
{
    public class TcpServerClient: ITcpServerClient
    {
        private string _clientIpAddress = string.Empty;
        private int _port;
        private readonly ILogger<TcpServerClient> _logger;
        
        public TcpServerClient(ILogger<TcpServerClient> logger)
        {
            _logger = logger;
        }

        public TcpClient? Client { get; set; }

        public bool Connected => Client?.Connected ?? false;
        public string RemoteEndPoint => _clientIpAddress;
        
        public async Task StartAsync()
        {
            if (Client == null)
            {
                await Task.FromException(new ArgumentNullException(nameof(Client)));
                return;
            }
            
            if (Client.Client.RemoteEndPoint is IPEndPoint endPoint)
            {
                _clientIpAddress = $"{endPoint.Address}:{endPoint.Port}";
            }
            
            if (Client.Client.LocalEndPoint is IPEndPoint p)
            {
                _port = p.Port;
            }
            _logger.LogInformation("Starting client {Ip} on port {Port}", _clientIpAddress, _port);
            var stream = Client.GetStream();
            Byte[] bytes = new Byte[256];
            int i;
            try
            {
                while ((i = await stream.ReadAsync(bytes, 0, bytes.Length)) != 0 && Client.Connected)
                {
                    await OnDataReceivedAsync(bytes.AsMemory(0, i), CancellationToken.None).ConfigureAwait(false);
                }
            }
            catch (Exception e)
            {
                _logger.LogError("Exception reading from client: {Exception}", e.ToString());
            }
            finally
            {
                Stop();
            }
        }

        public void Stop()
        {
            Client?.Close();
            OnConnectionClosed();
            _logger.LogInformation("Client {ClientIpAddress} on port {Port} Stopped", _clientIpAddress, _port);
        }
        
        public async ValueTask SendAsync(string data, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(data)) return;
            if (Client != null && Client.Connected)
            {
                Byte[] reply = Encoding.ASCII.GetBytes(data);
                await Client.GetStream().WriteAsync(reply, cancellationToken).ConfigureAwait(false);
            }
        }

        public event EventHandler<EventArgs>? ConnectionClosed;
        public event Func<ITcpServerClient, ReadOnlyMemory<byte>, CancellationToken, ValueTask>? DataReceived;

        private void OnConnectionClosed()
        {
            ConnectionClosed?.Invoke(this, EventArgs.Empty);
        }

        private async ValueTask OnDataReceivedAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken)
        {
            if (DataReceived is null)
            {
                return;
            }

            foreach (Func<ITcpServerClient, ReadOnlyMemory<byte>, CancellationToken, ValueTask> handler in DataReceived.GetInvocationList())
            {
                await handler(this, data, cancellationToken).ConfigureAwait(false);
            }
        }

        public override string ToString()
        {
            return $"Client {_clientIpAddress} on port {_port}";
        }
    }
}
