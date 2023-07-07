using System.Net.Sockets;
using FlexRadioServices.Events;

namespace FlexRadioServices.Models.Ports.Network;

public interface ITcpServerClient
{
    Task SendAsync(string message);

    event EventHandler<DataReceivedEventArgs> DataReceived;
    
    event EventHandler<EventArgs>? ConnectionClosed;
    
    bool Connected { get; }

    void Stop();

    TcpClient Client { get; internal set; }
}