using FlexRadioServices.Models.Settings;
using FlexRadioServices.Services;
using MQTTnet.Client;

namespace FlexRadioServices.Utils;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMqttClientHostedService(this IServiceCollection services)
    {
        if (!string.IsNullOrEmpty(AppSettings.MqttBrokerSettings.BrokerHost))
        {
            services.AddMqttClientServiceWithConfig(aspOptionBuilder =>
            {
                aspOptionBuilder
                    .WithCredentials(AppSettings.MqttBrokerSettings.ClientUser,
                        AppSettings.MqttBrokerSettings.ClientPassword)
                    .WithClientId(AppSettings.MqttBrokerSettings.ClientId)
                    .WithTcpServer(AppSettings.MqttBrokerSettings.BrokerHost, AppSettings.MqttBrokerSettings.BrokerPort);
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
        services.AddSingleton<IHostedService>(serviceProvider =>
        {
            return serviceProvider.GetService<MqttClientService>()!;
        });
        services.AddSingleton<IMqttClientService>(serviceProvider =>
        {
            var mqttClientService = serviceProvider.GetService<MqttClientService>();
            return mqttClientService;
        });
        return services;
    }
}