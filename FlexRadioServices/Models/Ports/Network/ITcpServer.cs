
using System.Net;

namespace FlexRadioServices.Models.Ports.Network;

public interface ITcpServer
{
    event EventHandler<ClientConnectedEventArgs> ClientConnected;

    event EventHandler<ClientDisconnectedEventArgs> ClientDisconnected;

    List<ITcpServerClient> Clients { get; }

    Task StartListener(IPAddress ip, int port, CancellationToken cancellationToken);
    
    string PortFriendlyName { get; set; }
}