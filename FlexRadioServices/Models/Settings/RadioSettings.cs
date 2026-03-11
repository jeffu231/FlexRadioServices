namespace FlexRadioServices.Models.Settings;

public class RadioSettings
{
    public bool AutoConnect { get; set; }

    public string? PreferredRadioIdentifier { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the duplex mute logic is enabled.
    /// This property controls whether the system activates duplex mute logic
    /// to work around a bug in the Flex firmware.
    /// </summary>
    public bool FullDuplexMuteLogicEnabled { get; set; }
}