using FlexRadioServices.Models.Settings;
using FlexRadioServices.Utils;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace FlexRadioServices.Tests.Models.Settings;

/// <summary>
/// Verifies startup validation for runtime configuration options.
/// </summary>
public sealed class RuntimeConfigurationOptionsTests
{
    [Fact]
    public void GetOptions_ValidConfiguration_BindsSuccessfully()
    {
        using var services = CreateServices(CreateValidValues());

        var catPorts = services.GetRequiredService<IOptions<CatPortSettings>>().Value;
        var mqtt = services.GetRequiredService<IOptions<MqttBrokerSettings>>().Value;
        var radio = services.GetRequiredService<IOptions<RadioSettings>>().Value;

        Assert.Equal((ushort)6005, Assert.Single(catPorts.PortSettings).PortNumber);
        Assert.Equal("mqtt.example.test", mqtt.BrokerHost);
        Assert.Equal("radio-1", radio.PreferredRadioIdentifier);
    }

    [Theory]
    [InlineData("CatPorts:PortSettings:1:PortNumber", "6005", "PortNumber 6005")]
    [InlineData("CatPorts:PortSettings:0:PortNumber", "0", "between 1 and 65535")]
    [InlineData("CatPorts:PortSettings:0:ClientId", " ", "ClientId is required")]
    [InlineData("CatPorts:PortSettings:0:VfoASliceLetter", "I", "VfoASliceLetter")]
    [InlineData("CatPorts:PortSettings:0:Protocol", "UDP", "Protocol must be TCP")]
    public void GetCatPortOptions_InvalidConfiguration_Throws(string key, string value, string expectedMessage)
    {
        var values = CreateValidValues();
        values[key] = value;
        using var services = CreateServices(values);

        var exception = Assert.Throws<OptionsValidationException>(() => services.GetRequiredService<IOptions<CatPortSettings>>().Value);

        Assert.Contains(expectedMessage, exception.Message);
    }

    [Theory]
    [InlineData("MqttBrokerSettings:BrokerPort", "0", "BrokerPort")]
    [InlineData("MqttBrokerSettings:RootTopic", " ", "RootTopic")]
    public void GetMqttOptions_InvalidConfiguration_ThrowsWithoutPassword(string key, string value, string expectedMessage)
    {
        const string password = "do-not-disclose";
        var values = CreateValidValues();
        values[key] = value;
        values["MqttBrokerSettings:ClientPassword"] = password;
        values["MqttBrokerSettings:ClientUser"] = "operator";
        using var services = CreateServices(values);

        var exception = Assert.Throws<OptionsValidationException>(() => services.GetRequiredService<IOptions<MqttBrokerSettings>>().Value);

        Assert.Contains(expectedMessage, exception.Message);
        Assert.DoesNotContain(password, exception.Message);
    }

    [Fact]
    public void GetRadioOptions_AutoConnectWithoutPreferredIdentifier_Throws()
    {
        var values = CreateValidValues();
        values["RadioSettings:PreferredRadioIdentifier"] = " ";
        using var services = CreateServices(values);

        var exception = Assert.Throws<OptionsValidationException>(() => services.GetRequiredService<IOptions<RadioSettings>>().Value);

        Assert.Contains("PreferredRadioIdentifier", exception.Message);
    }

    private static ServiceProvider CreateServices(IEnumerable<KeyValuePair<string, string?>> values)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(values).Build();
        return new ServiceCollection().AddRuntimeConfiguration(configuration).BuildServiceProvider();
    }

    private static Dictionary<string, string?> CreateValidValues() => new()
    {
        ["RadioSettings:AutoConnect"] = "true",
        ["RadioSettings:PreferredRadioIdentifier"] = "radio-1",
        ["MqttBrokerSettings:BrokerHost"] = "mqtt.example.test",
        ["MqttBrokerSettings:BrokerPort"] = "1883",
        ["MqttBrokerSettings:ClientId"] = "frs",
        ["MqttBrokerSettings:RootTopic"] = "flex",
        ["CatPorts:PortSettings:0:PortFriendlyName"] = "Test CAT",
        ["CatPorts:PortSettings:0:PortNumber"] = "6005",
        ["CatPorts:PortSettings:0:PortSliceType"] = "Designated",
        ["CatPorts:PortSettings:0:ClientId"] = "client-1",
        ["CatPorts:PortSettings:0:VfoASliceLetter"] = "A",
        ["CatPorts:PortSettings:0:VfoBSliceLetter"] = "B"
    };
}
