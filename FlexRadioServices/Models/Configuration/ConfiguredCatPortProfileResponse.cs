using System.Collections.Immutable;

namespace FlexRadioServices.Models.Configuration;

/// <summary>
/// Represents one reusable CAT listener profile saved at application startup.
/// </summary>
/// <param name="ProfileName">The profile identity.</param>
/// <param name="PortSettings">The listener settings owned by the profile.</param>
public sealed record ConfiguredCatPortProfileResponse(
    string ProfileName,
    ImmutableArray<ConfiguredPortSettingsResponse> PortSettings);
