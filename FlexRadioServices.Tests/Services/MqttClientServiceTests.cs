using FlexRadioServices.Models.Settings;
using FlexRadioServices.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using MQTTnet;
using MQTTnet.Client;
using Xunit;

namespace FlexRadioServices.Tests.Services;

/// <summary>
/// Verifies supervised MQTT connection, buffering, and shutdown behavior without a broker.
/// </summary>
public sealed class MqttClientServiceTests
{
    [Fact]
    public async Task StartAsync_OptionalBrokerRetriesAndPublishesLatestBufferedValue()
    {
        var connection = new FakeMqttClientConnection(failConnectionAttempts: 2);
        using var service = CreateService(connection);

        await service.PublishAsync("radios/1/meters/volts", "12.1", CancellationToken.None);
        await service.PublishAsync("radios/1/meters/volts", "12.2", CancellationToken.None);
        await service.StartAsync(CancellationToken.None);

        await WaitUntilAsync(() => connection.PublishedMessages.Count == 1, TimeSpan.FromSeconds(5));

        var message = Assert.Single(connection.PublishedMessages);
        Assert.Equal("flex/radios/1/meters/volts", message.Topic);
        Assert.Equal("12.2", message.ConvertPayloadToString());
        Assert.Equal(MQTTnet.Protocol.MqttQualityOfServiceLevel.AtMostOnce, message.QualityOfServiceLevel);
        Assert.False(message.Retain);
        Assert.True(service.Status.IsConnected);
        Assert.Equal(2, service.Status.RetryCount);
        Assert.Equal(0, service.Status.BufferedCount);
        await service.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task StartAsync_RequiredBrokerUnavailableFailsWithinConfiguredWindow()
    {
        var connection = new FakeMqttClientConnection(failConnectionAttempts: int.MaxValue);
        using var service = CreateService(connection, settings => settings with
        {
            Required = true,
            RequiredInitialConnectionTimeoutSeconds = 1
        });

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => service.StartAsync(CancellationToken.None));

        Assert.Contains("MQTT broker did not connect", exception.Message);
        await service.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task PublishAsync_BufferFullEvictsOldestTopic()
    {
        var connection = new FakeMqttClientConnection();
        using var service = CreateService(connection);
        await service.PublishAsync("radios/1/a", "first", CancellationToken.None);
        await service.PublishAsync("radios/1/b", "second", CancellationToken.None);
        await service.PublishAsync("radios/1/c", "third", CancellationToken.None);

        Assert.Equal(2, service.Status.BufferedCount);
        Assert.Equal(1, service.Status.DroppedCount);
        await service.StartAsync(CancellationToken.None);
        await WaitUntilAsync(() => connection.PublishedMessages.Count == 2, TimeSpan.FromSeconds(2));

        Assert.Collection(
            connection.PublishedMessages,
            message => Assert.Equal("flex/radios/1/b", message.Topic),
            message => Assert.Equal("flex/radios/1/c", message.Topic));
        await service.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task StopAsync_CancelsPendingConnectionAndDisposesClient()
    {
        var connection = new FakeMqttClientConnection(waitForCancellation: true);
        using var service = CreateService(connection);
        await service.StartAsync(CancellationToken.None);
        await WaitUntilAsync(() => connection.ConnectionStarted, TimeSpan.FromSeconds(2));

        using var shutdown = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        await service.StopAsync(shutdown.Token);

        Assert.True(connection.ConnectionCancellationObserved);
        service.Dispose();
        Assert.True(connection.Disposed);
    }

    private static MqttClientService CreateService(
        FakeMqttClientConnection connection,
        Func<MqttBrokerSettings, MqttBrokerSettings>? configure = null)
    {
        var settings = configure?.Invoke(new MqttBrokerSettings
        {
            BrokerHost = "mqtt.example.test",
            BrokerPort = 1883,
            ClientId = "frs-tests",
            RootTopic = "flex",
            OutboundCapacity = 2,
            RetryMinDelaySeconds = 1,
            RetryMaxDelaySeconds = 1
        }) ?? new MqttBrokerSettings
        {
            BrokerHost = "mqtt.example.test",
            BrokerPort = 1883,
            ClientId = "frs-tests",
            RootTopic = "flex",
            OutboundCapacity = 2,
            RetryMinDelaySeconds = 1,
            RetryMaxDelaySeconds = 1
        };
        var options = new MqttClientOptionsBuilder().WithTcpServer(settings.BrokerHost, settings.BrokerPort).Build();
        return new MqttClientService(
            new FakeMqttClientConnectionFactory(connection),
            options,
            Options.Create(settings),
            NullLogger<MqttClientService>.Instance);
    }

    private static async Task WaitUntilAsync(Func<bool> condition, TimeSpan timeout)
    {
        var deadline = DateTimeOffset.UtcNow + timeout;
        while (!condition())
        {
            if (DateTimeOffset.UtcNow >= deadline)
            {
                throw new TimeoutException("The expected MQTT service state was not reached.");
            }

            await Task.Delay(25);
        }
    }

    private sealed class FakeMqttClientConnectionFactory(FakeMqttClientConnection connection) : IMqttClientConnectionFactory
    {
        public IMqttClientConnection Create() => connection;
    }

    private sealed class FakeMqttClientConnection : IMqttClientConnection
    {
        private int _remainingFailures;
        private readonly bool _waitForCancellation;

        public FakeMqttClientConnection(int failConnectionAttempts = 0, bool waitForCancellation = false)
        {
            _remainingFailures = failConnectionAttempts;
            _waitForCancellation = waitForCancellation;
        }

        public bool ConnectionCancellationObserved { get; private set; }

        public bool ConnectionStarted { get; private set; }

        public bool Disposed { get; private set; }

        public bool IsConnected { get; private set; }

        public List<MqttApplicationMessage> PublishedMessages { get; } = [];

        public async Task ConnectAsync(MqttClientOptions options, CancellationToken cancellationToken)
        {
            ConnectionStarted = true;
            if (_waitForCancellation)
            {
                try
                {
                    await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    ConnectionCancellationObserved = true;
                    throw;
                }
            }

            if (Interlocked.Decrement(ref _remainingFailures) >= 0)
            {
                throw new InvalidOperationException("Broker unavailable.");
            }

            IsConnected = true;
        }

        public Task PublishAsync(MqttApplicationMessage message, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            PublishedMessages.Add(message);
            return Task.CompletedTask;
        }

        public Task DisconnectAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            IsConnected = false;
            return Task.CompletedTask;
        }

        public void Dispose() => Disposed = true;
    }
}
