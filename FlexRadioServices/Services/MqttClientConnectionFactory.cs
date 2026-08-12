using MQTTnet;

namespace FlexRadioServices.Services;

/// <summary>
/// Creates MQTTnet-backed broker connections.
/// </summary>
internal sealed class MqttClientConnectionFactory : IMqttClientConnectionFactory
{
    /// <inheritdoc />
    public IMqttClientConnection Create() => new MqttClientConnection(new MqttFactory().CreateMqttClient());
}
