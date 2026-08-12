namespace FlexRadioServices.Models.Radio;

/// <summary>
/// An immutable view of a GUI client connected to a radio.
/// </summary>
public sealed record RadioClientSnapshot(
    string ClientId,
    uint ClientHandle,
    string Station,
    string ProgramName,
    bool IsLocalPtt,
    string TransmitSliceLetter);
