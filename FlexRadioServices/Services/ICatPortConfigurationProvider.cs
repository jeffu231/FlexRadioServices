using System.Collections.Immutable;
using FlexRadioServices.Models.Settings;

namespace FlexRadioServices.Services;

/// <summary>
/// Provides the immutable CAT listener bindings resolved from startup configuration.
/// </summary>
public interface ICatPortConfigurationProvider
{
    /// <summary>
    /// Gets the CAT profiles configured when the application started.
    /// </summary>
    /// <returns>A copy of the configured CAT profiles.</returns>
    ImmutableArray<CatPortProfileSettings> GetConfiguredProfiles();

    /// <summary>
    /// Gets the CAT clients configured when the application started.
    /// </summary>
    /// <returns>A copy of the configured CAT clients.</returns>
    ImmutableArray<CatClientSettings> GetConfiguredClients();

    /// <summary>
    /// Gets the CAT listener bindings selected by enabled clients when the application started.
    /// </summary>
    /// <returns>The active CAT listener bindings in configured profile and port order.</returns>
    ImmutableArray<ResolvedCatPortBinding> GetActiveBindings();
}
