using System.Collections.Immutable;
using System.Net;
using System.Net.Sockets;
using FlexRadioServices.Models;
using FlexRadioServices.Models.Ports;
using FlexRadioServices.Models.Settings;
using FlexRadioServices.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace FlexRadioServices.Tests.Services;

public sealed class CatPortHostedServiceTests
{
    [Fact]
    public async Task StartAsync_NoActiveBindings_DoesNotCreateListeners()
    {
        var factory = new TestFactory();
        var service = CreateHostedService([], factory);

        await service.StartAsync(CancellationToken.None);
        await service.StopAsync(CancellationToken.None);

        Assert.Empty(factory.Bindings);
    }

    [Fact]
    public async Task StartAsync_ChildStartFails_StopsEveryCreatedChildAndRethrows()
    {
        var first = new TestCatPortService();
        var second = new TestCatPortService(new InvalidOperationException("bind failed"));
        var factory = new TestFactory(first, second);
        var service = CreateHostedService([CreateBinding(6101), CreateBinding(6102)], factory);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => service.StartAsync(CancellationToken.None));

        Assert.Equal("bind failed", exception.Message);
        Assert.Equal(1, first.StopCount);
        Assert.Equal(1, second.StopCount);
    }

    [Fact]
    public async Task ExecuteTask_ChildCompletesUnexpectedly_FaultsCoordinator()
    {
        var child = new TestCatPortService();
        var service = CreateHostedService([CreateBinding(6101)], new TestFactory(child));
        await service.StartAsync(CancellationToken.None);

        child.CompleteSuccessfully();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => service.ExecuteTask!);

        Assert.Equal("A CAT listener stopped unexpectedly.", exception.Message);
        await service.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task ExecuteTask_ChildFaults_PropagatesTheChildFailure()
    {
        var child = new TestCatPortService();
        var service = CreateHostedService([CreateBinding(6101)], new TestFactory(child));
        await service.StartAsync(CancellationToken.None);

        child.CompleteWithException(new InvalidOperationException("listener failed"));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => service.ExecuteTask!);

        Assert.Equal("listener failed", exception.Message);
        await service.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task StopAsync_ActiveChildren_StopsEveryChildOnce()
    {
        var first = new TestCatPortService();
        var second = new TestCatPortService();
        var service = CreateHostedService([CreateBinding(6101), CreateBinding(6102)], new TestFactory(first, second));
        await service.StartAsync(CancellationToken.None);

        await service.StopAsync(CancellationToken.None);

        Assert.Equal(1, first.StopCount);
        Assert.Equal(1, second.StopCount);
    }

    [Fact]
    public async Task StartAsync_ActiveBinding_OpensItsTcpListenerWithoutCreatingInactiveListeners()
    {
        var activePort = GetAvailablePort();
        using var inactiveReservation = new TcpListener(IPAddress.Loopback, 0);
        inactiveReservation.Start();
        var inactivePort = ((IPEndPoint)inactiveReservation.LocalEndpoint).Port;
        var activeChild = new LoopbackCatPortService(activePort);
        var factory = new TestFactory(activeChild);
        var service = CreateHostedService([CreateBinding((ushort)activePort)], factory);

        await service.StartAsync(CancellationToken.None);
        using var client = new TcpClient();
        await client.ConnectAsync(IPAddress.Loopback, activePort);

        Assert.Single(factory.Bindings);
        Assert.Equal((ushort)activePort, factory.Bindings[0].PortSettings.PortNumber);
        Assert.NotEqual((ushort)inactivePort, factory.Bindings[0].PortSettings.PortNumber);

        await service.StopAsync(CancellationToken.None);
    }

    private static CatPortHostedService CreateHostedService(
        ImmutableArray<ResolvedCatPortBinding> bindings,
        TestFactory factory) => new(
        new TestConfigurationProvider(bindings),
        factory,
        NullLogger<CatPortHostedService>.Instance);

    private static ResolvedCatPortBinding CreateBinding(ushort port) => new(
        "Operator",
        $"client-{port}",
        "Operator Client",
        new PortSettings
        {
            PortFriendlyName = $"CAT {port}",
            PortNumber = port,
            PortSliceType = PortSliceType.Active
        });

    private static int GetAvailablePort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        return ((IPEndPoint)listener.LocalEndpoint).Port;
    }

    private sealed class TestConfigurationProvider(ImmutableArray<ResolvedCatPortBinding> bindings) : ICatPortConfigurationProvider
    {
        public ImmutableArray<CatPortProfileSettings> GetConfiguredProfiles() => [];

        public ImmutableArray<CatClientSettings> GetConfiguredClients() => [];

        public ImmutableArray<ResolvedCatPortBinding> GetActiveBindings() => bindings;
    }

    private sealed class TestFactory(params ICatPortService[] services) : ICatPortServiceFactory
    {
        private readonly Queue<ICatPortService> _services = new(services);

        public List<ResolvedCatPortBinding> Bindings { get; } = [];

        public ICatPortService Create(ResolvedCatPortBinding binding)
        {
            Bindings.Add(binding);
            return _services.Dequeue();
        }
    }

    private sealed class TestCatPortService(Exception? startException = null) : ICatPortService
    {
        private readonly TaskCompletionSource _completion = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task? CompletionTask { get; private set; }

        public int StopCount { get; private set; }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            CompletionTask = _completion.Task;
            return startException is null ? Task.CompletedTask : Task.FromException(startException);
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            StopCount++;
            _completion.TrySetResult();
            return Task.CompletedTask;
        }

        public void CompleteSuccessfully() => _completion.TrySetResult();

        public void CompleteWithException(Exception exception) => _completion.TrySetException(exception);
    }

    private sealed class LoopbackCatPortService(int port) : ICatPortService
    {
        private readonly TcpListener _listener = new(IPAddress.Loopback, port);
        private readonly TaskCompletionSource _completion = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task? CompletionTask { get; private set; }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _listener.Start();
            CompletionTask = _completion.Task;
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _listener.Stop();
            _completion.TrySetResult();
            return Task.CompletedTask;
        }
    }
}
