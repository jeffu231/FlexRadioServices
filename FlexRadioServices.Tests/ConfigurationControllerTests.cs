using System.Text.Json;
using FlexRadioServices.Controllers;
using FlexRadioServices.Models.Configuration;
using FlexRadioServices.Models.Settings;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace FlexRadioServices.Tests;

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

    private static ConfigurationController CreateController(MqttBrokerSettings mqttBrokerSettings) => new(
        NullLogger<ConfigurationController>.Instance,
        Options.Create(new CatPortSettings()),
        Options.Create(mqttBrokerSettings),
        Options.Create(new RadioSettings()));

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
