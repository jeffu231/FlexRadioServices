namespace FlexRadioServices.Models.Configuration;

/// <summary>
/// Represents one CAT client saved at application startup.
/// </summary>
/// <param name="ClientId">The Flex GUI client identifier.</param>
/// <param name="ClientFriendlyName">The operator-facing client name.</param>
/// <param name="Enabled">Whether the client activates its profile at startup.</param>
/// <param name="ProfileName">The profile selected by the client.</param>
public sealed record ConfiguredCatClientResponse(
    string ClientId,
    string ClientFriendlyName,
    bool Enabled,
    string ProfileName);
