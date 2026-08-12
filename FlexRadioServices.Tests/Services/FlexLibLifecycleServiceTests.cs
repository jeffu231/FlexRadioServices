using System.Runtime.CompilerServices;
using Flex.Smoothlake.FlexLib;
using FlexRadioServices.Models.Settings;
using FlexRadioServices.Services;
using FlexRadioServices.Tests.Services.FlexLib;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace FlexRadioServices.Tests.Services;

/// <summary>
/// Verifies FlexLib initialization and cleanup under the host lifetime.
/// </summary>
public sealed class FlexLibLifecycleServiceTests
{
    [Fact]
    public async Task StartAsync_InitializesAndForwardsRadioEvents()
    {
        var flexLibApi = new FakeFlexLibApi();
        var readinessState = new ReadinessState();
        var flexRadioService = CreateRadioService(flexLibApi);
        var lifecycleService = CreateLifecycleService(flexLibApi, flexRadioService, readinessState);

        await lifecycleService.StartAsync(CancellationToken.None);
        await lifecycleService.StartAsync(CancellationToken.None);
        flexLibApi.RaiseRadioAdded((Radio)RuntimeHelpers.GetUninitializedObject(typeof(Radio)));

        Assert.True(readinessState.IsReady);
        Assert.Equal(1, flexLibApi.Operations.Count(operation => operation == "Initialize"));
        Assert.Equal(1, flexLibApi.RadioAddedSubscriberCount);
        Assert.Equal(1, flexLibApi.RadioRemovedSubscriberCount);
        Assert.Contains("SubscribeRadioAdded", flexLibApi.Operations);
    }

    [Fact]
    public async Task StopAsync_UnsubscribesRadioEventsBeforeClosingSession()
    {
        var flexLibApi = new FakeFlexLibApi();
        var readinessState = new ReadinessState();
        var lifecycleService = CreateLifecycleService(flexLibApi, CreateRadioService(flexLibApi), readinessState);
        await lifecycleService.StartAsync(CancellationToken.None);

        await lifecycleService.StopAsync(CancellationToken.None);

        Assert.False(readinessState.IsReady);
        Assert.Equal(0, flexLibApi.RadioAddedSubscriberCount);
        Assert.Equal(0, flexLibApi.RadioRemovedSubscriberCount);
        Assert.Equal(
            ["Initialize", "SubscribeRadioAdded", "SubscribeRadioRemoved", "UnsubscribeRadioAdded", "UnsubscribeRadioRemoved", "CloseSession"],
            flexLibApi.Operations);
    }

    [Fact]
    public async Task StopAsync_AfterFailedStart_IsSafeAndReadinessIsUnhealthy()
    {
        var flexLibApi = new FakeFlexLibApi { InitializeException = new InvalidOperationException("Vendor unavailable.") };
        var readinessState = new ReadinessState();
        var lifecycleService = CreateLifecycleService(flexLibApi, CreateRadioService(flexLibApi), readinessState);

        await lifecycleService.StartAsync(CancellationToken.None);

        var failedStartResult = await new FlexLibReadinessHealthCheck(readinessState)
            .CheckHealthAsync(new HealthCheckContext());
        await lifecycleService.StopAsync(CancellationToken.None);

        var result = await new FlexLibReadinessHealthCheck(readinessState)
            .CheckHealthAsync(new HealthCheckContext());
        Assert.Equal(HealthStatus.Unhealthy, failedStartResult.Status);
        Assert.Equal("FlexLib initialization failed.", failedStartResult.Description);
        Assert.Equal(HealthStatus.Unhealthy, result.Status);
        Assert.Equal("FlexLib is stopping.", result.Description);
        Assert.Equal(["Initialize", "CloseSession"], flexLibApi.Operations);
    }

    private static FlexRadioService CreateRadioService(FakeFlexLibApi flexLibApi) => new(
        NullLogger<FlexRadioService>.Instance,
        Options.Create(new RadioSettings()),
        flexLibApi);

    private static FlexLibLifecycleService CreateLifecycleService(
        FakeFlexLibApi flexLibApi,
        FlexRadioService flexRadioService,
        IReadinessState readinessState) => new(
        flexLibApi,
        flexRadioService,
        readinessState,
        NullLogger<FlexLibLifecycleService>.Instance);
}
