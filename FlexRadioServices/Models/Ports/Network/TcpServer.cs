using System.Net;
using System.Net.Sockets;

namespace FlexRadioServices.Models.Ports.Network
{
    public class TcpServer:ITcpServer
    {
        private TcpListener? _server;
        private bool _running;
        private readonly ILogger<TcpServer> _logger;
        private readonly IServiceProvider _serviceProvider;
        
        public TcpServer(ILogger<TcpServer> logger, IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            Clients = new List<ITcpServerClient>();
        }
        
        public async Task StartListener(IPAddress ip, int port, CancellationToken cancellationToken)
        {
            await Task.Factory.StartNew(() =>
            {
                try
                {
                    //IPAddress localAddr = IPAddress.Parse(ip);
                    _logger.LogDebug("Starting on {IP} and port {Port}", ip, port);
                    _server = new TcpListener(ip, port);
                    _server.Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, 1);
                    _server.Start();
                    _running = true;
                    while (_running && !cancellationToken.IsCancellationRequested)
                    {
                        _logger.LogDebug("Waiting for a connection on port {Port}", port);
                        TcpClient client = _server.AcceptTcpClient();
                        _logger.LogDebug("Connected on port {Port}!", port);
                        var networkClient = _serviceProvider.GetService<ITcpServerClient>();
                        if (networkClient != null)
                        {
                            networkClient.Client = client;
                            networkClient.ConnectionClosed += NetworkClientOnConnectionClosed;
                            Clients.Add(networkClient);
                            Task.Run(() => StartClient(networkClient), cancellationToken);
                        }
                    }
                    
                    StopListener();
                }
                catch (SocketException e)
                {
                    _logger.LogCritical(e, "Socket Exception starting server for port {Port}", port );
                }
            }, cancellationToken);

        }

        public string PortFriendlyName { get; set; } = String.Empty;

        private void NetworkClientOnConnectionClosed(object? sender, EventArgs e)
        {
            if (sender is TcpServerClient c)
            {
                _logger.LogInformation("Client {ClientInfo} is connected: {IsConnected}",c.ToString(), c.Connected);
                c.ConnectionClosed -= NetworkClientOnConnectionClosed;
                ClientDisconnected?.Invoke(this, new ClientDisconnectedEventArgs(c));
                Clients.Remove(c);
            }
        }

        private void StopListener()
        {
            StopClients();
            _server?.Stop();
            _running = false;
            _logger.LogDebug("Listener Stopped");
        }

        private void StopClients()
        {
            foreach (var client in Clients.ToList())
            {
                client.ConnectionClosed -= NetworkClientOnConnectionClosed;
                Clients.Remove(client);
                client.Stop();
            }
        }

        private void StartClient(Object? obj)
        {
            if (obj is TcpServerClient nc)
            {
                ClientConnected?.Invoke(this, new ClientConnectedEventArgs(nc));
#pragma warning disable CS4014
                nc.StartAsync();
#pragma warning restore CS4014
            }
        }

        public event EventHandler<ClientConnectedEventArgs>? ClientConnected;
        public event EventHandler<ClientDisconnectedEventArgs>? ClientDisconnected;
        public List<ITcpServerClient> Clients { get; }
    }
}