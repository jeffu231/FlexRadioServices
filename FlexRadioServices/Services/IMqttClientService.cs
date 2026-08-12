namespace FlexRadioServices.Services;

public interface IMqttClientService: IHostedService
{
    /// <summary>
    /// Publishes a message through the configured MQTT client.
    /// </summary>
    /// <param name="topic">The topic relative to the configured root topic.</param>
    /// <param name="value">The message payload.</param>
    /// <param name="cancellationToken">A token that cancels the publish operation.</param>
    /// <returns>A task that represents the asynchronous publish operation.</returns>
    Task PublishAsync(string topic, string value, CancellationToken cancellationToken);
}
