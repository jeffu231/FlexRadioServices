using System.Collections.Immutable;

namespace FlexRadioServices.Models.Configuration;

/// <summary>
/// Represents one CAT profile's effective listener state at application startup.
/// </summary>
/// <param name="ProfileName">The configured profile identity.</param>
/// <param name="IsActive">Whether an enabled client selected the profile at startup.</param>
/// <param name="ActiveClient">The enabled client, or <see langword="null"/> when the profile is inactive.</param>
/// <param name="Listeners">The active TCP listeners, or an empty collection when the profile is inactive.</param>
public sealed record EffectiveCatPortProfileResponse(
    string ProfileName,
    bool IsActive,
    ActiveCatClientResponse? ActiveClient,
    ImmutableArray<EffectiveCatPortListenerResponse> Listeners);
