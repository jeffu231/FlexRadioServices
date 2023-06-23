using FlexRadioServices.Models.Ports.Network;

namespace FlexRadioServices.Services;

public class CatCommand
{
    public CatCommand(string command, ITcpServerClient client)
    {
        Command = command;
        Client = client;
    }
    
    public string Command { get; init; }

    public ITcpServerClient Client { get; init; }
}