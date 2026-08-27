namespace FlexRadioServices.Models.Configuration;

/// <summary>
/// Represents the enabled client that activates a CAT profile.
/// </summary>
/// <param name="ClientId">The Flex GUI client identifier.</param>
/// <param name="ClientFriendlyName">The operator-facing client name.</param>
public sealed record ActiveCatClientResponse(string ClientId, string ClientFriendlyName);
