using System.Collections.Immutable;

namespace FlexRadioServices.Models.Configuration;

/// <summary>
/// Represents the CAT profiles and clients saved at application startup.
/// </summary>
/// <param name="Profiles">The reusable CAT listener profiles in configured order.</param>
/// <param name="Clients">The CAT clients in configured order.</param>
public sealed record ConfiguredCatPortSettingsResponse(
    ImmutableArray<ConfiguredCatPortProfileResponse> Profiles,
    ImmutableArray<ConfiguredCatClientResponse> Clients);
