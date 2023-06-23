namespace FlexRadioServices.Services;

public interface IMqttClientService: IHostedService
{
    Task Publish(string topic, string value);
}