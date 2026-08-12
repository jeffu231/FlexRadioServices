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

    /// <summary>
    /// Gets a value that indicates whether startup must fail when the configured broker is unavailable.
    /// </summary>
    /// <value><see langword="true"/> to require a broker connection during startup; otherwise, <see langword="false"/>. The default is <see langword="false"/>.</value>
    public bool Required { get; init; }

    /// <summary>
    /// Gets the maximum number of latest-value telemetry messages retained while disconnected.
    /// </summary>
    /// <value>The bounded outbound queue capacity. The default is 1024.</value>
    public int OutboundCapacity { get; init; } = 1024;

    /// <summary>
    /// Gets the minimum delay before retrying a failed MQTT connection.
    /// </summary>
    /// <value>The retry delay in seconds. The default is 1.</value>
    public int RetryMinDelaySeconds { get; init; } = 1;

    /// <summary>
    /// Gets the maximum delay before retrying a failed MQTT connection.
    /// </summary>
    /// <value>The retry delay in seconds. The default is 30.</value>
    public int RetryMaxDelaySeconds { get; init; } = 30;

    /// <summary>
    /// Gets the maximum time startup waits for a required MQTT broker connection.
    /// </summary>
    /// <value>The startup connection window in seconds. The default is 30.</value>
    public int RequiredInitialConnectionTimeoutSeconds { get; init; } = 30;

    /// <summary>
    /// Gets the outbound message handling behavior used while the broker is disconnected.
    /// </summary>
    /// <value>The delivery policy. The default is <see cref="MqttDeliveryPolicy.LatestValuePerTopic"/>.</value>
    public MqttDeliveryPolicy DeliveryPolicy { get; init; } = MqttDeliveryPolicy.LatestValuePerTopic;
}
