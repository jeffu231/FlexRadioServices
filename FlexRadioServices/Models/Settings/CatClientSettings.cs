namespace FlexRadioServices.Models.Settings;

/// <summary>
/// Represents a Flex GUI client and the CAT listener profile it selects at service startup.
/// </summary>
public sealed record CatClientSettings
{
    /// <summary>
    /// Gets or sets the Flex GUI client identifier.
    /// </summary>
    public string ClientId { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the operator-facing name of the Flex GUI client.
    /// </summary>
    public string ClientFriendlyName { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets a value that indicates whether this client activates its selected profile at startup.
    /// </summary>
    public bool Enabled { get; init; }

    /// <summary>
    /// Gets or sets the name of the profile selected by this client.
    /// </summary>
    public string ProfileName { get; init; } = string.Empty;
}
