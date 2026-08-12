using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace FlexRadioServices.Services;

/// <summary>
/// Reports whether FlexLib completed startup successfully.
/// </summary>
internal sealed class FlexLibReadinessHealthCheck(IReadinessState readinessState) : IHealthCheck
{
    /// <inheritdoc />
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default) =>
        Task.FromResult(readinessState.IsReady
            ? HealthCheckResult.Healthy("FlexLib is initialized.")
            : HealthCheckResult.Unhealthy(readinessState.FailureDescription ?? "FlexLib is unavailable."));
}
