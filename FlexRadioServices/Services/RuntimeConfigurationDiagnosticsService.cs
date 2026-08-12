using FlexRadioServices.Models.Settings;
using Microsoft.Extensions.Options;

namespace FlexRadioServices.Services;

/// <summary>
/// Logs the nonsecret runtime configuration that is effective until the next service restart.
/// </summary>
public sealed class RuntimeConfigurationDiagnosticsService(
    ILogger<RuntimeConfigurationDiagnosticsService> logger,
    IOptions<CatPortSettings> catPortSettings,
    IOptions<MqttBrokerSettings> mqttBrokerSettings,
    IOptions<RadioSettings> radioSettings) : IHostedService
{
    /// <inheritdoc />
    public Task StartAsync(CancellationToken cancellationToken)
    {
        var ports = string.Join(',', catPortSettings.Value.PortSettings.Select(port => port.PortNumber));
        var mqttHost = string.IsNullOrWhiteSpace(mqttBrokerSettings.Value.BrokerHost)
            ? "disabled"
            : mqttBrokerSettings.Value.BrokerHost;

        logger.LogInformation(
            "Runtime configuration is loaded at startup and requires restart to change. Auto-connect: {AutoConnect}; CAT ports: {CatPorts}; MQTT host: {MqttHost}",
            radioSettings.Value.AutoConnect,
            ports,
            mqttHost);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
