namespace FlexRadioServices.Services;

/// <summary>
/// Publishes MQTT telemetry and reports the broker delivery state.
/// </summary>
public interface IMqttClientService : IHostedService
{
    /// <summary>
    /// Gets the current MQTT connection and delivery counters.
    /// </summary>
    MqttConnectionStatus Status { get; }

    /// <summary>
    /// Queues a message for publishing through the configured MQTT client.
    /// </summary>
    /// <param name="topic">The topic relative to the configured root topic.</param>
    /// <param name="value">The message payload.</param>
    /// <param name="cancellationToken">A token that cancels queuing the message.</param>
    /// <returns>A task that represents the asynchronous queue operation.</returns>
    Task PublishAsync(string topic, string value, CancellationToken cancellationToken);
}
