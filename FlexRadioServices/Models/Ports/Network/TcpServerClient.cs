using System.Net;
using System.Net.Sockets;
using System.Text;
using FlexRadioServices.Events;

namespace FlexRadioServices.Models.Ports.Network
{
    public class TcpServerClient: ITcpServerClient
    {
        private string _clientIpAddress = string.Empty;
        private readonly ILogger<TcpServerClient> _logger;
        
        public TcpServerClient(ILogger<TcpServerClient> logger)
        {
            _logger = logger;
        }

        public TcpClient? Client { get; set; }

        public bool Connected => Client?.Connected ?? false;
        
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
            
            _logger.LogInformation("Starting client {Ip}", _clientIpAddress);
            var stream = Client.GetStream();
            Byte[] bytes = new Byte[256];
            int i;
            try
            {
                while ((i = await stream.ReadAsync(bytes, 0, bytes.Length)) != 0 && Client.Connected)
                {
                    string data = Encoding.ASCII.GetString(bytes, 0, i);
                    OnDataReceived(data);
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
            _logger.LogInformation("Client {ClientIpAddress} Stopped", _clientIpAddress);
        }
        
        public async Task SendAsync(string data)
        {
            if (Client != null && Client.Connected)
            {
                Byte[] reply = Encoding.ASCII.GetBytes(data);
                await Client.GetStream().WriteAsync(reply);
            }
        }

        public event EventHandler<EventArgs>? ConnectionClosed;
        public event EventHandler<DataReceivedEventArgs>? DataReceived;

        private void OnConnectionClosed()
        {
            ConnectionClosed?.Invoke(this, new EventArgs());
        }

        private void OnDataReceived(string data)
        {
            DataReceived?.Invoke(this, new DataReceivedEventArgs {Data = data});
        }
        
    }
}