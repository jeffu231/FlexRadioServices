using FlexRadioServices.Models.Settings;
using Microsoft.Extensions.Options;

namespace FlexRadioServices.Services;

/// <summary>
/// Logs the nonsecret runtime configuration that is effective until the next service restart.
/// </summary>
public sealed class RuntimeConfigurationDiagnosticsService(
    ILogger<RuntimeConfigurationDiagnosticsService> logger,
    ICatPortConfigurationProvider catPortConfigurationProvider,
    IOptions<MqttBrokerSettings> mqttBrokerSettings,
    IOptions<RadioSettings> radioSettings) : IHostedService
{
    /// <inheritdoc />
    public Task StartAsync(CancellationToken cancellationToken)
    {
        var profiles = catPortConfigurationProvider.GetConfiguredProfiles();
        var clients = catPortConfigurationProvider.GetConfiguredClients();
        var activeBindings = catPortConfigurationProvider.GetActiveBindings();
        var ports = activeBindings.IsEmpty
            ? "disabled"
            : string.Join(',', activeBindings.Select(binding => binding.PortSettings.PortNumber));
        var mqttHost = string.IsNullOrWhiteSpace(mqttBrokerSettings.Value.BrokerHost)
            ? "disabled"
            : mqttBrokerSettings.Value.BrokerHost;

        logger.LogInformation(
            "Runtime configuration is loaded at startup and requires restart to change. Auto-connect: {AutoConnect}; configured CAT profiles: {CatProfileCount}; configured CAT clients: {CatClientCount}; active CAT ports: {CatPorts}; MQTT host: {MqttHost}",
            radioSettings.Value.AutoConnect,
            profiles.Length,
            clients.Length,
            ports,
            mqttHost);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
