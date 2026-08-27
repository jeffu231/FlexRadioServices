namespace FlexRadioServices.Models.Settings;

/// <summary>
/// Represents one CAT listener resolved from an enabled client and its selected profile.
/// </summary>
/// <param name="ProfileName">The name of the profile that owns the listener.</param>
/// <param name="ClientId">The identifier of the Flex GUI client selected at startup.</param>
/// <param name="ClientFriendlyName">The operator-facing name of the selected Flex GUI client.</param>
/// <param name="PortSettings">The settings for the CAT TCP listener.</param>
public sealed record ResolvedCatPortBinding(
    string ProfileName,
    string ClientId,
    string ClientFriendlyName,
    PortSettings PortSettings);
