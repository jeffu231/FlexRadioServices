using Flex.Smoothlake.FlexLib;

namespace FlexRadioServices.Models;

public class RadioClientProxy
{
    public RadioClientProxy(GUIClient client)
    {
        ClientId = client.ClientID;
        ClientHandle = client.ClientHandle;
        Station = client.Station;
        ProgramName = client.Program;
        IsLocalPtt = client.IsLocalPtt;
        TransmitSliceLetter = client.TransmitSlice.Letter;
    }
    public string ClientId { get; }
    public uint ClientHandle { get; }
    public string Station { get; }
    public string ProgramName { get; }
    public bool IsLocalPtt { get; }
    public string TransmitSliceLetter { get; }

}