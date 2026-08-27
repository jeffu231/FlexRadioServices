using System.Collections.Immutable;
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

    private sealed class TestConfigurationProvider(ImmutableArray<ResolvedCatPortBinding> bindings) : ICatPortConfigurationProvider
    {
        public ImmutableArray<CatPortProfileSettings> GetConfiguredProfiles() => [];

        public ImmutableArray<CatClientSettings> GetConfiguredClients() => [];

        public ImmutableArray<ResolvedCatPortBinding> GetActiveBindings() => bindings;
    }

    private sealed class TestFactory(params TestCatPortService[] services) : ICatPortServiceFactory
    {
        private readonly Queue<TestCatPortService> _services = new(services);

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
}
