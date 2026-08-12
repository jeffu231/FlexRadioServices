using FlexRadioServices.Services.FlexLib;

namespace FlexRadioServices.Services;

/// <summary>
/// Manages FlexLib initialization and cleanup within the ASP.NET Core host lifetime.
/// </summary>
public sealed class FlexLibLifecycleService(
    IFlexLibApi flexLibApi,
    FlexRadioService flexRadioService,
    IReadinessState readinessState,
    ILogger<FlexLibLifecycleService> logger) : IHostedService
{
    private bool _startAttempted;

    /// <inheritdoc />
    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (_startAttempted)
        {
            return Task.CompletedTask;
        }

        _startAttempted = true;
        try
        {
            flexLibApi.Initialize();
            flexRadioService.Initialize();
            readinessState.MarkReady();
            logger.LogInformation("FlexLib initialized successfully.");
        }
        catch (Exception exception)
        {
            readinessState.MarkUnready("FlexLib initialization failed.");
            logger.LogError(exception, "FlexLib initialization failed; readiness will remain unavailable.");
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken)
    {
        try
        {
            flexRadioService.Stop();
        }
        finally
        {
            readinessState.MarkUnready("FlexLib is stopping.");
            flexLibApi.CloseSession();
            logger.LogInformation("FlexLib session closed.");
        }

        return Task.CompletedTask;
    }
}
