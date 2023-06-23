namespace FlexRadioServices.Models.Ports.Network;

public class ClientConnectedEventArgs:EventArgs
{
    public ClientConnectedEventArgs(ITcpServerClient client)
    {
        Client = client;
    }
    
    public ITcpServerClient Client { get; init; }
}