using MQTTnet;
using MQTTnet.Client;

namespace FlexRadioServices.Services;

/// <summary>
/// Represents the MQTTnet operations owned by the supervised MQTT worker.
/// </summary>
public interface IMqttClientConnection : IDisposable
{
    /// <summary>
    /// Gets a value that indicates whether the broker connection is active.
    /// </summary>
    bool IsConnected { get; }

    /// <summary>
    /// Connects to the broker using the supplied options.
    /// </summary>
    Task ConnectAsync(MqttClientOptions options, CancellationToken cancellationToken);

    /// <summary>
    /// Publishes an MQTT message through the active connection.
    /// </summary>
    Task PublishAsync(MqttApplicationMessage message, CancellationToken cancellationToken);

    /// <summary>
    /// Disconnects from the broker.
    /// </summary>
    Task DisconnectAsync(CancellationToken cancellationToken);
}
