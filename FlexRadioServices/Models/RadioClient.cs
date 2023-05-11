namespace FlexRadioServices.Models;

public class RadioClient
{
   
    public RadioClient(string clientId, uint clientHandle, string station, string programName)
    {
        ClientId = clientId;
        ClientHandle = clientHandle;
        Station = station;
        ProgramName = programName;
    }
    public string ClientId { get; }
    public uint ClientHandle { get; }
    public string Station { get; }
    public string ProgramName { get; }

}