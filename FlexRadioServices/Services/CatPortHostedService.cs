using FlexRadioServices.Models.Ports;
using FlexRadioServices.Models.Settings;

namespace FlexRadioServices.Services;

/// <summary>
/// Coordinates the lifetime of every CAT listener selected by startup configuration.
/// </summary>
public sealed class CatPortHostedService(
    ICatPortConfigurationProvider configurationProvider,
    ICatPortServiceFactory serviceFactory,
    ILogger<CatPortHostedService> logger) : BackgroundService
{
    private readonly ICatPortConfigurationProvider _configurationProvider = configurationProvider ?? throw new ArgumentNullException(nameof(configurationProvider));
    private readonly ICatPortServiceFactory _serviceFactory = serviceFactory ?? throw new ArgumentNullException(nameof(serviceFactory));
    private readonly ILogger<CatPortHostedService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly SemaphoreSlim _lifecycleLock = new(1, 1);
    private readonly List<(ResolvedCatPortBinding Binding, ICatPortService Service)> _children = [];

    /// <inheritdoc />
    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        await _lifecycleLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            foreach (var binding in _configurationProvider.GetActiveBindings())
            {
                var service = _serviceFactory.Create(binding);
                _children.Add((binding, service));
                _logger.LogInformation(
                    "Starting CAT listener for profile {ProfileName}, client {ClientId}, port {PortNumber}",
                    binding.ProfileName,
                    binding.ClientId,
                    binding.PortSettings.PortNumber);
                await service.StartAsync(cancellationToken).ConfigureAwait(false);

                if (service.CompletionTask is null)
                {
                    throw new InvalidOperationException(
                        $"CAT listener for profile '{binding.ProfileName}' and port {binding.PortSettings.PortNumber} did not expose a completion task after startup.");
                }
            }

            await base.StartAsync(cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            await StopChildrenAsync(CancellationToken.None).ConfigureAwait(false);
            throw;
        }
        finally
        {
            _lifecycleLock.Release();
        }
    }

    /// <inheritdoc />
    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        try
        {
            await base.StopAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            await _lifecycleLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                await StopChildrenAsync(cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                _lifecycleLock.Release();
            }
        }
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var completionTasks = _children
            .Select(child => child.Service.CompletionTask
                ?? throw new InvalidOperationException("A started CAT listener did not expose a completion task."))
            .ToArray();
        var stoppingTask = Task.Delay(Timeout.InfiniteTimeSpan, stoppingToken);

        if (completionTasks.Length == 0)
        {
            try
            {
                await stoppingTask.ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
            }

            return;
        }

        var childCompletionTask = Task.WhenAny(completionTasks);
        var completedTask = await Task.WhenAny(childCompletionTask, stoppingTask).ConfigureAwait(false);
        if (ReferenceEquals(completedTask, stoppingTask))
        {
            return;
        }

        var childTask = await childCompletionTask.ConfigureAwait(false);
        await childTask.ConfigureAwait(false);
        throw new InvalidOperationException("A CAT listener stopped unexpectedly.");
    }

    private async Task StopChildrenAsync(CancellationToken cancellationToken)
    {
        var children = _children.ToArray();
        _children.Clear();
        if (children.Length == 0)
        {
            return;
        }

        foreach (var (binding, _) in children)
        {
            _logger.LogInformation(
                "Stopping CAT listener for profile {ProfileName}, client {ClientId}, port {PortNumber}",
                binding.ProfileName,
                binding.ClientId,
                binding.PortSettings.PortNumber);
        }

        await Task.WhenAll(children.Select(child => child.Service.StopAsync(cancellationToken))).ConfigureAwait(false);
    }
}
