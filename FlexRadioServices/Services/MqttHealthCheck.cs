using Microsoft.Extensions.Diagnostics.HealthChecks;
using FlexRadioServices.Models.Settings;
using Microsoft.Extensions.Options;

namespace FlexRadioServices.Services;

/// <summary>
/// Reports the MQTT broker's readiness according to the configured required-broker policy.
/// </summary>
internal sealed class MqttHealthCheck(
    IMqttClientService mqttClientService,
    IOptions<MqttBrokerSettings> settings) : IHealthCheck
{
    /// <inheritdoc />
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var status = mqttClientService.Status;
        if (status.IsConnected)
        {
            return Task.FromResult(HealthCheckResult.Healthy("MQTT broker is connected."));
        }

        var data = new Dictionary<string, object>
        {
            ["lastSuccessfulConnection"] = status.LastSuccessfulConnection?.ToString("O") ?? "never",
            ["retryCount"] = status.RetryCount,
            ["bufferedCount"] = status.BufferedCount,
            ["droppedCount"] = status.DroppedCount
        };
        return Task.FromResult(settings.Value.Required
            ? HealthCheckResult.Unhealthy("Required MQTT broker is disconnected.", data: data)
            : HealthCheckResult.Degraded("MQTT broker is disconnected.", data: data));
    }
}
