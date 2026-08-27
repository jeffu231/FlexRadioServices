namespace FlexRadioServices.Models.Settings;

/// <summary>
/// Represents a reusable named set of CAT TCP listener settings.
/// </summary>
public sealed record CatPortProfileSettings
{
    /// <summary>
    /// Gets or sets the operator-defined profile identity.
    /// </summary>
    public string ProfileName { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the CAT TCP listeners owned by this profile.
    /// </summary>
    public List<PortSettings> PortSettings { get; init; } = [];
}
