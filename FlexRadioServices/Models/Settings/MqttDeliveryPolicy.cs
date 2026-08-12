namespace FlexRadioServices.Models.Settings;

/// <summary>
/// Defines how outbound MQTT telemetry is retained while the broker is unavailable.
/// </summary>
public enum MqttDeliveryPolicy
{
    /// <summary>
    /// Retains only the most recent payload for each full MQTT topic in a bounded buffer.
    /// </summary>
    LatestValuePerTopic
}
