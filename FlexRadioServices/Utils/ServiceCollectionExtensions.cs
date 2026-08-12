using FlexRadioServices.Models.Settings;
using FlexRadioServices.Services;
using Microsoft.Extensions.Options;
using MQTTnet.Client;

namespace FlexRadioServices.Utils;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers startup-validated runtime configuration settings.
    /// </summary>
    /// <param name="services">The service collection receiving the options registrations.</param>
    /// <param name="configuration">The application configuration containing the settings sections.</param>
    /// <returns>The service collection with the options registrations.</returns>
    public static IServiceCollection AddRuntimeConfiguration(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<RadioSettings>()
            .Bind(configuration.GetSection(RadioSettings.SectionName))
            .ValidateDataAnnotations()
            .Services.AddSingleton<IValidateOptions<RadioSettings>, RadioSettingsValidator>();
        services.AddOptions<MqttBrokerSettings>()
            .Bind(configuration.GetSection(MqttBrokerSettings.SectionName))
            .ValidateDataAnnotations()
            .Services.AddSingleton<IValidateOptions<MqttBrokerSettings>, MqttBrokerSettingsValidator>();
        services.AddOptions<CatPortSettings>()
            .Bind(configuration.GetSection(CatPortSettings.SectionName))
            .ValidateDataAnnotations()
            .Services.AddSingleton<IValidateOptions<CatPortSettings>, CatPortSettingsValidator>();

        services.AddOptions<RadioSettings>().ValidateOnStart();
        services.AddOptions<MqttBrokerSettings>().ValidateOnStart();
        services.AddOptions<CatPortSettings>().ValidateOnStart();
        return services;
    }

    public static IServiceCollection AddMqttClientHostedService(this IServiceCollection services, MqttBrokerSettings mqttBrokerSettings)
    {
        if (!string.IsNullOrWhiteSpace(mqttBrokerSettings.BrokerHost))
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
        services.AddSingleton<IMqttClientConnectionFactory, MqttClientConnectionFactory>();
        services.AddSingleton<IHostedService>(serviceProvider => serviceProvider.GetService<MqttClientService>()!);
        services.AddSingleton<IMqttClientService>(serviceProvider =>
        {
            var mqttClientService = serviceProvider.GetService<MqttClientService>();
            return mqttClientService ?? throw new InvalidOperationException();
        });
        return services;
    }
}
