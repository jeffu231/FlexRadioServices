using Flex.Smoothlake.FlexLib;

namespace FlexRadioServices.Models;

public sealed class RadioClientProxy
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RadioClientProxy"/> class.
    /// </summary>
    /// <param name="client">The GUI client to map.</param>
    public RadioClientProxy(GUIClient client)
    {
        ArgumentNullException.ThrowIfNull(client);

        ClientId = client.ClientID;
        ClientHandle = client.ClientHandle;
        Station = client.Station;
        ProgramName = client.Program;
        IsLocalPtt = client.IsLocalPtt;
        TransmitSliceLetter = client.TransmitSlice?.Letter;
    }

    /// <summary>Gets the GUI client identifier.</summary>
    public string ClientId { get; }

    /// <summary>Gets the GUI client handle.</summary>
    public uint ClientHandle { get; }

    /// <summary>Gets the station name reported by the GUI client.</summary>
    public string Station { get; }

    /// <summary>Gets the program name reported by the GUI client.</summary>
    public string ProgramName { get; }

    /// <summary>Gets a value that indicates whether the client owns local PTT.</summary>
    public bool IsLocalPtt { get; }

    /// <summary>
    /// Gets the current transmit-slice letter, or <see langword="null"/> when the client has no transmit slice.
    /// </summary>
    public string? TransmitSliceLetter { get; }

}
