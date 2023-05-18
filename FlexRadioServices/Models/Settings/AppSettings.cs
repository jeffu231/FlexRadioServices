namespace FlexRadioServices.Models.Settings;

public class AppSettings
{
    public static RadioSettings RadioSettings { get; set; } = new RadioSettings();

    public static MqttBrokerSettings MqttBrokerSettings { get; set; } = new MqttBrokerSettings();
}