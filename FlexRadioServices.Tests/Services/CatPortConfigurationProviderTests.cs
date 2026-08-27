using FlexRadioServices.Models;
using FlexRadioServices.Models.Settings;
using FlexRadioServices.Services;
using Microsoft.Extensions.Options;
using Xunit;

namespace FlexRadioServices.Tests.Services;

public sealed class CatPortConfigurationProviderTests
{
    [Fact]
    public void GetActiveBindings_EnabledClients_UsesProfileAndPortOrderWithCaseInsensitiveLookup()
    {
        var provider = new CatPortConfigurationProvider(Options.Create(new CatPortSettings
        {
            Profiles =
            [
                CreateProfile("First", 6101, 6102),
                CreateProfile("Second", 6103)
            ],
            Clients =
            [
                new CatClientSettings
                {
                    ClientId = "client-second",
                    ClientFriendlyName = "Second Client",
                    Enabled = true,
                    ProfileName = "SECOND"
                },
                new CatClientSettings
                {
                    ClientId = "client-first",
                    ClientFriendlyName = "First Client",
                    Enabled = true,
                    ProfileName = "first"
                }
            ]
        }));

        var bindings = provider.GetActiveBindings();

        Assert.Collection(bindings,
            binding =>
            {
                Assert.Equal("First", binding.ProfileName);
                Assert.Equal("client-first", binding.ClientId);
                Assert.Equal((ushort)6101, binding.PortSettings.PortNumber);
            },
            binding => Assert.Equal((ushort)6102, binding.PortSettings.PortNumber),
            binding =>
            {
                Assert.Equal("Second", binding.ProfileName);
                Assert.Equal("client-second", binding.ClientId);
                Assert.Equal((ushort)6103, binding.PortSettings.PortNumber);
            });
    }

    [Fact]
    public void GetActiveBindings_DisabledAndUnusedProfiles_ReturnsNoBindings()
    {
        var provider = new CatPortConfigurationProvider(Options.Create(new CatPortSettings
        {
            Profiles = [CreateProfile("Disabled", 6101), CreateProfile("Unused", 6102)],
            Clients =
            [
                new CatClientSettings
                {
                    ClientId = "disabled-client",
                    ClientFriendlyName = "Disabled Client",
                    Enabled = false,
                    ProfileName = "Disabled"
                }
            ]
        }));

        var bindings = provider.GetActiveBindings();

        Assert.Empty(bindings);
    }

    [Fact]
    public void GetConfiguredProfiles_ReturnsCopiesThatCannotMutateStartupSnapshot()
    {
        var provider = new CatPortConfigurationProvider(Options.Create(new CatPortSettings
        {
            Profiles = [CreateProfile("Operator", 6101)]
        }));

        var returnedProfile = Assert.Single(provider.GetConfiguredProfiles());
        returnedProfile.PortSettings.Clear();

        Assert.Single(Assert.Single(provider.GetConfiguredProfiles()).PortSettings);
    }

    private static CatPortProfileSettings CreateProfile(string name, params ushort[] ports) => new()
    {
        ProfileName = name,
        PortSettings = ports.Select(port => new PortSettings
        {
            PortFriendlyName = $"CAT {port}",
            PortNumber = port,
            PortSliceType = PortSliceType.Active
        }).ToList()
    };
}
