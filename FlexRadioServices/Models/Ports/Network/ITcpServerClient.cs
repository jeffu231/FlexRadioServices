using FlexRadioServices.Events;

namespace FlexRadioServices.Models.Ports.Network;

public interface ITcpServerClient
{
    Task SendAsync(string message);

    event EventHandler<DataReceivedEventArgs> DataReceived;
    
    bool Connected { get; }
}