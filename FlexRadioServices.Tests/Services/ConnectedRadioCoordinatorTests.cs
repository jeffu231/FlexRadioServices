using System.Runtime.CompilerServices;
using FlexRadioServices.Models;
using FlexRadioServices.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace FlexRadioServices.Tests.Services;

public sealed class ConnectedRadioCoordinatorTests
{
    [Fact]
    public async Task ExecuteAsync_AttachesListenersToInitiallyConnectedRadio()
    {
        var radio = CreateRadioProxy();
        var coordinator = new FakeConnectedRadioCoordinator(radio);
        var service = new TestConnectedRadioService(coordinator);
        using var cancellationSource = new CancellationTokenSource();

        var execution = service.RunAsync(cancellationSource.Token);
        await service.WaitForTransitionAsync();

        Assert.Equal([radio], service.AddedRadios);
        Assert.Equal([null], service.PreviousRadios);

        cancellationSource.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => execution);
    }

    [Fact]
    public async Task ConnectedRadioCoordinator_ReplacementRemovesBeforeAttachingNewRadio()
    {
        var firstRadio = CreateRadioProxy();
        var secondRadio = CreateRadioProxy();
        var coordinator = new FakeConnectedRadioCoordinator(firstRadio);
        var service = new TestConnectedRadioService(coordinator);
        using var cancellationSource = new CancellationTokenSource();

        var execution = service.RunAsync(cancellationSource.Token);
        await service.WaitForTransitionAsync();
        coordinator.Publish(new ConnectedRadioTransition(firstRadio, secondRadio));

        Assert.Equal([firstRadio, firstRadio, secondRadio], service.ListenerOperations);
        Assert.Equal([firstRadio], service.RemovedRadios);
        Assert.Equal([null, firstRadio], service.PreviousRadios);

        cancellationSource.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => execution);
    }

    [Fact]
    public async Task ConnectedRadioCoordinator_RemovalDetachesFormerRadioOnce()
    {
        var radio = CreateRadioProxy();
        var coordinator = new FakeConnectedRadioCoordinator(radio);
        var service = new TestConnectedRadioService(coordinator);
        using var cancellationSource = new CancellationTokenSource();

        var execution = service.RunAsync(cancellationSource.Token);
        await service.WaitForTransitionAsync();
        coordinator.Publish(new ConnectedRadioTransition(radio, null));

        Assert.Equal([radio], service.RemovedRadios);
        Assert.Equal([null, radio], service.PreviousRadios);

        cancellationSource.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => execution);
    }

    private static RadioProxy CreateRadioProxy() =>
        (RadioProxy)RuntimeHelpers.GetUninitializedObject(typeof(RadioProxy));

    private sealed class FakeConnectedRadioCoordinator(RadioProxy? connectedRadio) : IConnectedRadioCoordinator
    {
        public event EventHandler<ConnectedRadioTransition>? ConnectedRadioChanged;

        public RadioProxy? GetConnectedRadioHandle() => connectedRadio;

        public void Publish(ConnectedRadioTransition transition)
        {
            connectedRadio = transition.CurrentRadio;
            ConnectedRadioChanged?.Invoke(this, transition);
        }
    }

    private sealed class TestConnectedRadioService(IConnectedRadioCoordinator coordinator)
        : ConnectedRadioServiceBase(coordinator, NullLogger.Instance)
    {
        private readonly TaskCompletionSource _initialTransition = new();

        public List<RadioProxy> AddedRadios { get; } = [];

        public List<RadioProxy> RemovedRadios { get; } = [];

        public List<RadioProxy?> PreviousRadios { get; } = [];

        public List<RadioProxy> ListenerOperations { get; } = [];

        public Task RunAsync(CancellationToken cancellationToken) => ExecuteAsync(cancellationToken);

        public Task WaitForTransitionAsync() => _initialTransition.Task;

        protected override Task DoWorkAsync(CancellationToken cancellationToken) =>
            Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);

        protected override void AddRadioListeners(RadioProxy radio)
        {
            AddedRadios.Add(radio);
            ListenerOperations.Add(radio);
        }

        protected override void RemoveRadioListeners(RadioProxy radio)
        {
            RemovedRadios.Add(radio);
            ListenerOperations.Add(radio);
        }

        protected override void ConnectedRadioChanged(object? sender, ConnectedRadioEventArgs args)
        {
            PreviousRadios.Add(args.PreviousRadio);
            _initialTransition.TrySetResult();
        }
    }
}
