using Microsoft.Extensions.Options;

namespace FlexRadioServices.Models.Settings;

/// <summary>
/// Validates MQTT broker settings before the service starts.
/// </summary>
public sealed class MqttBrokerSettingsValidator : IValidateOptions<MqttBrokerSettings>
{
    /// <inheritdoc />
    public ValidateOptionsResult Validate(string? name, MqttBrokerSettings options)
    {
        if (string.IsNullOrWhiteSpace(options.BrokerHost))
        {
            return ValidateOptionsResult.Success;
        }

        var failures = new List<string>();
        if (options.BrokerPort is < 1 or > 65535)
        {
            failures.Add($"{MqttBrokerSettings.SectionName}: BrokerPort must be between 1 and 65535 when MQTT is enabled.");
        }

        if (string.IsNullOrWhiteSpace(options.ClientId))
        {
            failures.Add($"{MqttBrokerSettings.SectionName}: ClientId is required when MQTT is enabled.");
        }

        if (string.IsNullOrWhiteSpace(options.RootTopic))
        {
            failures.Add($"{MqttBrokerSettings.SectionName}: RootTopic is required when MQTT is enabled.");
        }

        if (string.IsNullOrWhiteSpace(options.ClientUser) != string.IsNullOrWhiteSpace(options.ClientPassword))
        {
            failures.Add($"{MqttBrokerSettings.SectionName}: ClientUser and ClientPassword must either both be configured or both be empty.");
        }

        return failures.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(failures);
    }
}
