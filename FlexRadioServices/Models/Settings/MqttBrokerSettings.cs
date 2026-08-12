using System.Text.Json.Serialization;

namespace FlexRadioServices.Models.Settings;

/// <summary>
/// Represents the MQTT broker settings read during service startup.
/// </summary>
public sealed record MqttBrokerSettings
{
    /// <summary>
    /// Gets the configuration section name for MQTT broker settings.
    /// </summary>
    public const string SectionName = "MqttBrokerSettings";

    /// <summary>
    /// Gets or sets the MQTT broker host. An empty value disables MQTT publishing.
    /// </summary>
    public string BrokerHost { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the MQTT broker TCP port.
    /// </summary>
    public int BrokerPort { get; init; }

    /// <summary>
    /// Gets or sets the MQTT client identifier.
    /// </summary>
    public string ClientId { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the MQTT client user name.
    /// </summary>
    public string ClientUser { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the MQTT client password.
    /// </summary>
    [JsonIgnore]
    public string ClientPassword { get; init; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the MQTT topic under which radio state is published.
    /// </summary>
    public string RootTopic { get; init; } = string.Empty;
}
