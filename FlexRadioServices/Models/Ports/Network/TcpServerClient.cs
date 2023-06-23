using System.Net;
using System.Net.Sockets;
using System.Text;
using FlexRadioServices.Events;
using System;

namespace FlexRadioServices.Models.Ports.Network
{
    public class TcpServerClient: ITcpServerClient
    {
        private readonly TcpClient _client;
        private readonly string _clientIpAddress = string.Empty;
        
        public TcpServerClient(TcpClient client)
        {
            _client = client;
            if (client.Client.RemoteEndPoint is IPEndPoint endPoint)
            {
                _clientIpAddress = $"{endPoint.Address}:{endPoint.Port}";
            }
        }

        public bool Connected => _client.Connected;
        
        public async Task StartAsync()
        {
            Console.WriteLine("Starting client {0}", _clientIpAddress);
            var stream = _client.GetStream();
            Byte[] bytes = new Byte[256];
            int i;
            try
            {
                while ((i = await stream.ReadAsync(bytes, 0, bytes.Length)) != 0 && _client.Connected)
                {
                    string data = Encoding.ASCII.GetString(bytes, 0, i);
                    OnDataReceived(data);
                    
                    //Console.WriteLine($"Received: {data} from {_clientIpAddress} at {DateTime.Now.ToLongTimeString()}");
                    //var str = _catDataProvider.HandleCommand(data.Trim());
                    //Send(str);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception: {0}", e.ToString());
            }
            finally
            {
                Stop();
            }
        }

        public void Stop()
        {
            //_catDataProvider.CatDataChanged -= CatDataProviderOnCatDataChanged;
            _client.Close();
            OnConnectionClosed();
            Console.Out.WriteLine($"Client {_clientIpAddress} Stopped");
        }
        
        public async Task SendAsync(string data)
        {
            if (_client.Connected)
            {
                Byte[] reply = Encoding.ASCII.GetBytes(data);
                await _client.GetStream().WriteAsync(reply);
                //Console.WriteLine($"Sent: {data} to {_clientIpAddress} at {DateTime.Now.ToLongTimeString()}");
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
            //Console.Out.WriteLine($"OnDataRecieved: {data}");
            DataReceived?.Invoke(this, new DataReceivedEventArgs {Data = data});
        }

        
    }
}