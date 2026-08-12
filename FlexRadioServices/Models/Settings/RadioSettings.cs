namespace FlexRadioServices.Models.Settings;

/// <summary>
/// Represents the radio connection settings read during service startup.
/// </summary>
public sealed record RadioSettings
{
    /// <summary>
    /// Gets the configuration section name for radio settings.
    /// </summary>
    public const string SectionName = "RadioSettings";

    /// <summary>
    /// Gets or sets a value that indicates whether the preferred radio is connected automatically.
    /// </summary>
    public bool AutoConnect { get; init; }

    /// <summary>
    /// Gets or sets the identifier of the radio to connect when auto-connect is enabled.
    /// </summary>
    public string? PreferredRadioIdentifier { get; init; }

    /// <summary>
    /// Gets or sets a value indicating whether the duplex mute logic is enabled.
    /// This property controls whether the system activates duplex mute logic
    /// to work around a bug in the Flex firmware.
    /// </summary>
    public bool FullDuplexMuteLogicEnabled { get; init; }
}
