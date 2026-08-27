using System.Collections.Immutable;
using FlexRadioServices.Models.Settings;
using Microsoft.Extensions.Options;

namespace FlexRadioServices.Services;

/// <summary>
/// Resolves validated CAT configuration once for the lifetime of the application.
/// </summary>
public sealed class CatPortConfigurationProvider : ICatPortConfigurationProvider
{
    private readonly ImmutableArray<CatPortProfileSettings> _profiles;
    private readonly ImmutableArray<CatClientSettings> _clients;
    private readonly ImmutableArray<ResolvedCatPortBinding> _activeBindings;

    /// <summary>
    /// Initializes a new instance of the <see cref="CatPortConfigurationProvider"/> class.
    /// </summary>
    /// <param name="catPortSettings">The startup-validated CAT settings.</param>
    public CatPortConfigurationProvider(IOptions<CatPortSettings> catPortSettings)
    {
        ArgumentNullException.ThrowIfNull(catPortSettings);

        var settings = catPortSettings.Value;
        _profiles = settings.Profiles.Select(CloneProfile).ToImmutableArray();
        _clients = settings.Clients.Select(client => client with { }).ToImmutableArray();
        _activeBindings = ResolveActiveBindings(_profiles, _clients);
    }

    /// <inheritdoc />
    public ImmutableArray<CatPortProfileSettings> GetConfiguredProfiles() => _profiles.Select(CloneProfile).ToImmutableArray();

    /// <inheritdoc />
    public ImmutableArray<CatClientSettings> GetConfiguredClients() => _clients.Select(client => client with { }).ToImmutableArray();

    /// <inheritdoc />
    public ImmutableArray<ResolvedCatPortBinding> GetActiveBindings() => _activeBindings
        .Select(binding => binding with { PortSettings = binding.PortSettings with { } })
        .ToImmutableArray();

    private static ImmutableArray<ResolvedCatPortBinding> ResolveActiveBindings(
        ImmutableArray<CatPortProfileSettings> profiles,
        ImmutableArray<CatClientSettings> clients)
    {
        var enabledClientsByProfile = new Dictionary<string, CatClientSettings>(StringComparer.OrdinalIgnoreCase);
        foreach (var client in clients)
        {
            if (client.Enabled)
            {
                enabledClientsByProfile.Add(client.ProfileName, client);
            }
        }

        var bindings = ImmutableArray.CreateBuilder<ResolvedCatPortBinding>();
        foreach (var profile in profiles)
        {
            if (!enabledClientsByProfile.TryGetValue(profile.ProfileName, out var client))
            {
                continue;
            }

            foreach (var portSettings in profile.PortSettings)
            {
                bindings.Add(new ResolvedCatPortBinding(
                    profile.ProfileName,
                    client.ClientId,
                    client.ClientFriendlyName,
                    portSettings with { }));
            }
        }

        return bindings.ToImmutable();
    }

    private static CatPortProfileSettings CloneProfile(CatPortProfileSettings profile) => profile with
    {
        PortSettings = profile.PortSettings.Select(portSettings => portSettings with { }).ToList()
    };
}
