using MQTTnet;
using MQTTnet.Client;

namespace FlexRadioServices.Services;

/// <summary>
/// Adapts MQTTnet's client to the narrow connection contract used by the MQTT worker.
/// </summary>
internal sealed class MqttClientConnection(IMqttClient client) : IMqttClientConnection
{
    /// <inheritdoc />
    public bool IsConnected => client.IsConnected;

    /// <inheritdoc />
    public Task ConnectAsync(MqttClientOptions options, CancellationToken cancellationToken)
        => client.ConnectAsync(options, cancellationToken);

    /// <inheritdoc />
    public Task PublishAsync(MqttApplicationMessage message, CancellationToken cancellationToken)
        => client.PublishAsync(message, cancellationToken);

    /// <inheritdoc />
    public Task DisconnectAsync(CancellationToken cancellationToken)
        => client.DisconnectAsync(cancellationToken: cancellationToken);

    /// <inheritdoc />
    public void Dispose() => client.Dispose();
}
