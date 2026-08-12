namespace FlexRadioServices.Models.Configuration;

/// <summary>
/// Represents the MQTT settings that may be safely returned by the Configuration API.
/// </summary>
/// <remarks>
/// Does not include the MQTT password. <see cref="CredentialsConfigured"/> reports whether a non-whitespace
/// password is configured.
/// </remarks>
public sealed record MqttSettingsResponse(
    string BrokerHost,
    int BrokerPort,
    string ClientId,
    string ClientUser,
    string RootTopic,
    bool CredentialsConfigured);
