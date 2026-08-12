namespace FlexRadioServices.Services;

/// <summary>
/// Creates the MQTT connection owned by a <see cref="MqttClientService"/> instance.
/// </summary>
public interface IMqttClientConnectionFactory
{
    /// <summary>
    /// Creates a broker connection for one MQTT service lifetime.
    /// </summary>
    IMqttClientConnection Create();
}
