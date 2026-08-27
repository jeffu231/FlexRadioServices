using System.Collections.Immutable;

namespace FlexRadioServices.Models.Configuration;

/// <summary>
/// Represents the configured and effective CAT listener configuration returned by the API.
/// </summary>
/// <param name="Configured">The CAT profiles and clients saved at application startup.</param>
/// <param name="EffectiveProfiles">The profile activation state and active listeners at application startup.</param>
public sealed record CatPortSettingsResponse(
    ConfiguredCatPortSettingsResponse Configured,
    ImmutableArray<EffectiveCatPortProfileResponse> EffectiveProfiles);
