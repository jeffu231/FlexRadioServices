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
    /// Gets or sets the reusable CAT listener profiles.
    /// </summary>
    public List<CatPortProfileSettings> Profiles { get; init; } = [];

    /// <summary>
    /// Gets or sets the CAT clients that select listener profiles at service startup.
    /// </summary>
    public List<CatClientSettings> Clients { get; init; } = [];
}
