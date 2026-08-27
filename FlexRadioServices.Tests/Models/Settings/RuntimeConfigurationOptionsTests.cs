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

        var profile = Assert.Single(catPorts.Profiles);
        Assert.Equal("Operator", profile.ProfileName);
        Assert.Equal((ushort)6005, Assert.Single(profile.PortSettings).PortNumber);
        Assert.Equal("client-1", Assert.Single(catPorts.Clients).ClientId);
        Assert.Equal("mqtt.example.test", mqtt.BrokerHost);
        Assert.Equal("radio-1", radio.PreferredRadioIdentifier);
    }

    [Fact]
    public void GetCatPortOptions_EmptyConfiguration_DisablesCatWithoutValidationFailure()
    {
        var values = CreateValidValues();
        foreach (var key in values.Keys.Where(key => key.StartsWith("CatPorts:", StringComparison.Ordinal)).ToArray())
        {
            values.Remove(key);
        }

        using var services = CreateServices(values);

        var catPorts = services.GetRequiredService<IOptions<CatPortSettings>>().Value;

        Assert.Empty(catPorts.Profiles);
        Assert.Empty(catPorts.Clients);
    }

    [Fact]
    public void GetCatPortOptions_AllClientsDisabled_BindsSuccessfully()
    {
        var values = CreateValidValues();
        values["CatPorts:Clients:0:Enabled"] = "false";
        using var services = CreateServices(values);

        var client = Assert.Single(services.GetRequiredService<IOptions<CatPortSettings>>().Value.Clients);

        Assert.False(client.Enabled);
    }

    [Theory]
    [InlineData("CatPorts:Profiles:1:ProfileName", "operator", "Profiles:1:ProfileName")]
    [InlineData("CatPorts:Profiles:0:PortSettings:0:PortNumber", "0", "Profiles:0:PortSettings:0:PortNumber")]
    [InlineData("CatPorts:Clients:0:ClientId", " ", "Clients:0:ClientId is required")]
    [InlineData("CatPorts:Profiles:0:PortSettings:0:VfoASliceLetter", "I", "VfoASliceLetter")]
    [InlineData("CatPorts:Profiles:0:PortSettings:0:Protocol", "UDP", "Protocol must be TCP")]
    public void GetCatPortOptions_InvalidConfiguration_Throws(string key, string value, string expectedMessage)
    {
        var values = CreateValidValues();
        values[key] = value;
        using var services = CreateServices(values);

        var exception = Assert.Throws<OptionsValidationException>(() => services.GetRequiredService<IOptions<CatPortSettings>>().Value);

        Assert.Contains(expectedMessage, exception.Message);
    }

    [Fact]
    public void GetCatPortOptions_MultipleFailures_AggregatesPathQualifiedFailures()
    {
        var values = CreateValidValues();
        values["CatPorts:Clients:0:ClientFriendlyName"] = " ";
        values["CatPorts:Clients:0:ProfileName"] = "missing";
        values["CatPorts:Profiles:0:PortSettings:0:PortNumber"] = "0";
        using var services = CreateServices(values);

        var exception = Assert.Throws<OptionsValidationException>(() => services.GetRequiredService<IOptions<CatPortSettings>>().Value);

        Assert.Contains("CatPorts:Clients:0:ClientFriendlyName", exception.Message);
        Assert.Contains("CatPorts:Clients:0:ProfileName", exception.Message);
        Assert.Contains("CatPorts:Profiles:0:PortSettings:0:PortNumber", exception.Message);
    }

    [Fact]
    public void GetCatPortOptions_TwoEnabledClientsForProfile_Throws()
    {
        var values = CreateValidValues();
        values["CatPorts:Clients:1:ClientId"] = "client-2";
        values["CatPorts:Clients:1:ClientFriendlyName"] = "Second Client";
        values["CatPorts:Clients:1:Enabled"] = "true";
        values["CatPorts:Clients:1:ProfileName"] = "operator";
        using var services = CreateServices(values);

        var exception = Assert.Throws<OptionsValidationException>(() => services.GetRequiredService<IOptions<CatPortSettings>>().Value);

        Assert.Contains("enabled clients for profile", exception.Message);
    }

    [Fact]
    public void GetCatPortOptions_CaseInsensitiveDuplicateClientId_Throws()
    {
        var values = CreateValidValues();
        values["CatPorts:Clients:1:ClientId"] = "CLIENT-1";
        values["CatPorts:Clients:1:ClientFriendlyName"] = "Duplicate Client";
        values["CatPorts:Clients:1:Enabled"] = "false";
        values["CatPorts:Clients:1:ProfileName"] = "Operator";
        using var services = CreateServices(values);

        var exception = Assert.Throws<OptionsValidationException>(() => services.GetRequiredService<IOptions<CatPortSettings>>().Value);

        Assert.Contains("CatPorts:Clients:1:ClientId", exception.Message);
        Assert.Contains("ignoring case", exception.Message);
    }

    [Fact]
    public void GetCatPortOptions_ProfileWithoutPorts_Throws()
    {
        var values = CreateValidValues();
        values["CatPorts:Profiles:1:ProfileName"] = "Empty";
        using var services = CreateServices(values);

        var exception = Assert.Throws<OptionsValidationException>(() => services.GetRequiredService<IOptions<CatPortSettings>>().Value);

        Assert.Contains("CatPorts:Profiles:1:PortSettings", exception.Message);
    }

    [Fact]
    public void GetCatPortOptions_DuplicatePortInInactiveProfile_Throws()
    {
        var values = CreateValidValues();
        values["CatPorts:Profiles:1:ProfileName"] = "Inactive";
        values["CatPorts:Profiles:1:PortSettings:0:PortFriendlyName"] = "Inactive CAT";
        values["CatPorts:Profiles:1:PortSettings:0:PortNumber"] = "6005";
        values["CatPorts:Profiles:1:PortSettings:0:PortSliceType"] = "Active";
        using var services = CreateServices(values);

        var exception = Assert.Throws<OptionsValidationException>(() => services.GetRequiredService<IOptions<CatPortSettings>>().Value);

        Assert.Contains("PortNumber 6005 is configured more than once", exception.Message);
        Assert.Contains("CatPorts:Profiles:1:PortSettings:0:PortNumber", exception.Message);
    }

    [Fact]
    public void AddRuntimeConfiguration_LegacyCatPortSettingsKey_ThrowsDuringBinding()
    {
        var values = CreateValidValues();
        values["CatPorts:PortSettings:0:PortFriendlyName"] = "Legacy CAT";
        using var services = CreateServices(values);

        var exception = Assert.Throws<InvalidOperationException>(() => services.GetRequiredService<IOptions<CatPortSettings>>().Value);

        Assert.Contains("PortSettings", exception.Message);
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
        ["CatPorts:Profiles:0:ProfileName"] = "Operator",
        ["CatPorts:Profiles:0:PortSettings:0:PortFriendlyName"] = "Test CAT",
        ["CatPorts:Profiles:0:PortSettings:0:PortNumber"] = "6005",
        ["CatPorts:Profiles:0:PortSettings:0:PortSliceType"] = "Designated",
        ["CatPorts:Profiles:0:PortSettings:0:VfoASliceLetter"] = "A",
        ["CatPorts:Profiles:0:PortSettings:0:VfoBSliceLetter"] = "B",
        ["CatPorts:Clients:0:ClientId"] = "client-1",
        ["CatPorts:Clients:0:ClientFriendlyName"] = "Operator GUI",
        ["CatPorts:Clients:0:Enabled"] = "true",
        ["CatPorts:Clients:0:ProfileName"] = "operator"
    };
}
