namespace FlexRadioServices.Models.Radio;

/// <summary>
/// Identifies a slice owned by a GUI client on a radio.
/// </summary>
/// <param name="RadioId">The serial identifier of the radio.</param>
/// <param name="ClientHandle">The handle of the owning GUI client.</param>
/// <param name="Letter">The letter assigned to the slice.</param>
public sealed record SliceIdentity(string RadioId, uint ClientHandle, string Letter);
