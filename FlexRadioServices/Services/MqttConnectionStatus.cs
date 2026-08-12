namespace FlexRadioServices.Services;

/// <summary>
/// Represents the observable state of MQTT broker connectivity and outbound delivery.
/// </summary>
public sealed record MqttConnectionStatus(
    bool IsConnected,
    DateTimeOffset? LastSuccessfulConnection,
    long RetryCount,
    long BufferedCount,
    long DroppedCount);
