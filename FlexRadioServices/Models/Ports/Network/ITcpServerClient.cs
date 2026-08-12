using System.Net.Sockets;

namespace FlexRadioServices.Models.Ports.Network;

public interface ITcpServerClient
{
    ValueTask SendAsync(string message, CancellationToken cancellationToken);

    event Func<ITcpServerClient, ReadOnlyMemory<byte>, CancellationToken, ValueTask>? DataReceived;
    
    event EventHandler<EventArgs>? ConnectionClosed;
    
    bool Connected { get; }

    string RemoteEndPoint { get; }

    void Stop();

    TcpClient? Client { get; internal set; }
}
