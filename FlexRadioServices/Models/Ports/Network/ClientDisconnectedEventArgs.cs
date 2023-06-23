namespace FlexRadioServices.Models.Ports.Network;

public class ClientDisconnectedEventArgs
{
    public ClientDisconnectedEventArgs(ITcpServerClient client)
    {
        Client = client;
    }
    
    public ITcpServerClient Client { get; init; }
}