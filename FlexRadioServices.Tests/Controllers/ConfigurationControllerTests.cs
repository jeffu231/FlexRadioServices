using System.Text.Json;
using FlexRadioServices.Controllers;
using FlexRadioServices.Models;
using FlexRadioServices.Models.Configuration;
using FlexRadioServices.Models.Settings;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using FlexRadioServices.Services;
using Xunit;

namespace FlexRadioServices.Tests.Controllers;

public sealed class ConfigurationControllerTests
{
    private const string Secret = "unique-test-secret";

    [Fact]
    public async Task GetMqttBrokerSettings_RedactsPasswordAndReportsConfiguredCredentials()
    {
        var settings = CreateMqttBrokerSettings(Secret);
        var controller = CreateController(settings);

        var result = await controller.GetMqttBrokerSettings();
        var response = Assert.IsType<OkObjectResult>(result).Value;
        var json = JsonSerializer.Serialize(response, new JsonSerializerOptions(JsonSerializerDefaults.Web));

        var mqttResponse = Assert.IsType<MqttSettingsResponse>(response);
        Assert.True(mqttResponse.CredentialsConfigured);
        Assert.DoesNotContain(Secret, json, StringComparison.Ordinal);
        Assert.DoesNotContain("clientPassword", json, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("credentialsConfigured", json, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetMqttBrokerSettings_ReportsCredentialsNotConfiguredForEmptyOrWhitespacePassword(string password)
    {
        var controller = CreateController(CreateMqttBrokerSettings(password));

        var result = await controller.GetMqttBrokerSettings();
        var response = Assert.IsType<OkObjectResult>(result).Value;

        Assert.False(Assert.IsType<MqttSettingsResponse>(response).CredentialsConfigured);
    }

    [Fact]
    public void MqttBrokerSettings_SerializationDoesNotEmitPassword()
    {
        var json = JsonSerializer.Serialize(CreateMqttBrokerSettings(Secret));

        Assert.DoesNotContain(Secret, json, StringComparison.Ordinal);
        Assert.DoesNotContain("clientPassword", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void GetCatPortSettings_ReturnsConfiguredAndEffectiveProfileState()
    {
        var controller = CreateController(CreateMqttBrokerSettings(string.Empty), new CatPortSettings
        {
            Profiles =
            [
                CreateProfile("Active", 6101),
                CreateProfile("Inactive", 6102)
            ],
            Clients =
            [
                new CatClientSettings
                {
                    ClientId = "client-active",
                    ClientFriendlyName = "Active Client",
                    Enabled = true,
                    ProfileName = "active"
                },
                new CatClientSettings
                {
                    ClientId = "client-inactive",
                    ClientFriendlyName = "Inactive Client",
                    Enabled = false,
                    ProfileName = "Inactive"
                }
            ]
        });

        var result = controller.GetCatPortSettings();
        var response = Assert.IsType<OkObjectResult>(result.Result).Value;

        var settings = Assert.IsType<CatPortSettingsResponse>(response);
        Assert.Equal(2, settings.Configured.Profiles.Length);
        Assert.Equal(2, settings.Configured.Clients.Length);
        Assert.Collection(settings.EffectiveProfiles,
            profile =>
            {
                Assert.True(profile.IsActive);
                Assert.Equal("client-active", profile.ActiveClient?.ClientId);
                Assert.Equal((ushort)6101, Assert.Single(profile.Listeners).PortNumber);
            },
            profile =>
            {
                Assert.False(profile.IsActive);
                Assert.Null(profile.ActiveClient);
                Assert.Empty(profile.Listeners);
            });

        var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        Assert.Contains("configured", json, StringComparison.Ordinal);
        Assert.Contains("effectiveProfiles", json, StringComparison.Ordinal);
    }

    private static ConfigurationController CreateController(
        MqttBrokerSettings mqttBrokerSettings,
        CatPortSettings? catPortSettings = null) => new(
        new CatPortConfigurationProvider(Options.Create(catPortSettings ?? new CatPortSettings())),
        Options.Create(mqttBrokerSettings),
        Options.Create(new RadioSettings()));

    private static CatPortProfileSettings CreateProfile(string profileName, ushort portNumber) => new()
    {
        ProfileName = profileName,
        PortSettings =
        [
            new PortSettings
            {
                PortFriendlyName = $"CAT {portNumber}",
                PortNumber = portNumber,
                PortSliceType = PortSliceType.Active
            }
        ]
    };

    private static MqttBrokerSettings CreateMqttBrokerSettings(string password) => new()
    {
        BrokerHost = "broker",
        BrokerPort = 1883,
        ClientId = "frs",
        ClientUser = "operator",
        ClientPassword = password,
        RootTopic = "flex"
    };
}
