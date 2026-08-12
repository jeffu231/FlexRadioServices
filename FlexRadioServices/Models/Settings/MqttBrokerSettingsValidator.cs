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

        if (options.OutboundCapacity < 1)
        {
            failures.Add($"{MqttBrokerSettings.SectionName}: OutboundCapacity must be at least 1 when MQTT is enabled.");
        }

        if (options.RetryMinDelaySeconds < 1 || options.RetryMaxDelaySeconds < options.RetryMinDelaySeconds)
        {
            failures.Add($"{MqttBrokerSettings.SectionName}: retry delays must be positive and RetryMaxDelaySeconds must not be less than RetryMinDelaySeconds.");
        }

        if (options.RequiredInitialConnectionTimeoutSeconds < 1)
        {
            failures.Add($"{MqttBrokerSettings.SectionName}: RequiredInitialConnectionTimeoutSeconds must be at least 1 when MQTT is enabled.");
        }

        if (options.DeliveryPolicy != MqttDeliveryPolicy.LatestValuePerTopic)
        {
            failures.Add($"{MqttBrokerSettings.SectionName}: DeliveryPolicy must be LatestValuePerTopic.");
        }

        return failures.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(failures);
    }
}
