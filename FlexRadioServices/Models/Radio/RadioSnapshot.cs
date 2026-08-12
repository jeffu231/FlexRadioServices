namespace FlexRadioServices.Models.Radio;

/// <summary>
/// An immutable, application-owned view of a discovered radio.
/// </summary>
public sealed record RadioSnapshot(
    string Ip,
    string BranchName,
    string Model,
    string Nickname,
    string Callsign,
    string Serial,
    string Version,
    bool Connected,
    string ConnectedState,
    string Status,
    int CommandPort,
    bool IsWan,
    string BoundClientId,
    uint ClientHandle,
    string GuiClientId,
    string? TransmitSlice,
    uint TxClientHandle);
