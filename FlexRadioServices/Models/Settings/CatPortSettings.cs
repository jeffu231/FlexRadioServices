namespace FlexRadioServices.Models.Settings;

/// <summary>
/// Represents the CAT TCP listener settings read during service startup.
/// </summary>
public sealed record CatPortSettings
{
    /// <summary>
    /// Gets the configuration section name for CAT port settings.
    /// </summary>
    public const string SectionName = "CatPorts";

    /// <summary>
    /// Gets or sets the configured CAT TCP listeners.
    /// </summary>
    public List<PortSettings> PortSettings { get; init; } = [];
}
