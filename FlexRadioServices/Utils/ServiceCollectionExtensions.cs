using FlexRadioServices.Models.Settings;
using FlexRadioServices.Services;
using MQTTnet.Client;

namespace FlexRadioServices.Utils;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMqttClientHostedService(this IServiceCollection services, MqttBrokerSettings mqttBrokerSettings)
    {
        if (!string.IsNullOrEmpty(mqttBrokerSettings.BrokerHost))
        {
            services.AddMqttClientServiceWithConfig(aspOptionBuilder =>
            {
                aspOptionBuilder
                    .WithCredentials(mqttBrokerSettings.ClientUser,
                        mqttBrokerSettings.ClientPassword)
                    .WithClientId(mqttBrokerSettings.ClientId)
                    .WithTcpServer(mqttBrokerSettings.BrokerHost, mqttBrokerSettings.BrokerPort);
            });
        }
        
        return services;
    }

    private static IServiceCollection AddMqttClientServiceWithConfig(this IServiceCollection services, Action<MqttClientOptionsBuilder> configure)
    {
        services.AddSingleton<MqttClientOptions>(serviceProvider =>
        {
            var optionBuilder = new MqttClientOptionsBuilder();
            configure(optionBuilder);
            return optionBuilder.Build();
        });
        services.AddSingleton<MqttClientService>();
        services.AddSingleton<IHostedService>(serviceProvider => serviceProvider.GetService<MqttClientService>()!);
        services.AddSingleton<IMqttClientService>(serviceProvider =>
        {
            var mqttClientService = serviceProvider.GetService<MqttClientService>();
            return mqttClientService ?? throw new InvalidOperationException();
        });
        return services;
    }
}