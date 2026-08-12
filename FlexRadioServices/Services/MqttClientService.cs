using FlexRadioServices.Models.Settings;
using Microsoft.Extensions.Options;
using MQTTnet;
using MQTTnet.Client;

namespace FlexRadioServices.Services;

/// <summary>
/// Manages a single supervised MQTT connection and bounded latest-value delivery queue.
/// </summary>
public sealed class MqttClientService : BackgroundService, IMqttClientService
{
    private readonly IMqttClientConnection _connection;
    private readonly MqttClientOptions _options;
    private readonly MqttBrokerSettings _settings;
    private readonly ILogger<MqttClientService> _logger;
    private readonly object _sync = new();
    private readonly Dictionary<string, PendingMqttMessage> _pendingMessages = new(StringComparer.Ordinal);
    private readonly LinkedList<string> _pendingTopics = new();
    private readonly SemaphoreSlim _signal = new(0, 1);
    private readonly TaskCompletionSource _initialConnection = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private bool _isConnected;
    private DateTimeOffset? _lastSuccessfulConnection;
    private long _retryCount;
    private long _droppedCount;
    private long _bufferedCount;

    /// <summary>
    /// Initializes a new instance of the <see cref="MqttClientService"/> class.
    /// </summary>
    public MqttClientService(
        IMqttClientConnectionFactory connectionFactory,
        MqttClientOptions options,
        IOptions<MqttBrokerSettings> settings,
        ILogger<MqttClientService> logger)
    {
        ArgumentNullException.ThrowIfNull(connectionFactory);
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(logger);

        _connection = connectionFactory.Create();
        _options = options;
        _settings = settings.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    public MqttConnectionStatus Status
    {
        get
        {
            lock (_sync)
            {
                return new MqttConnectionStatus(
                    _isConnected,
                    _lastSuccessfulConnection,
                    _retryCount,
                    _bufferedCount,
                    _droppedCount);
            }
        }
    }

    /// <inheritdoc />
    public Task PublishAsync(string topic, string value, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(topic);
        ArgumentNullException.ThrowIfNull(value);
        cancellationToken.ThrowIfCancellationRequested();

        var fullTopic = $"{_settings.RootTopic}/{topic}";
        lock (_sync)
        {
            if (_pendingMessages.TryGetValue(fullTopic, out var existing))
            {
                _pendingMessages[fullTopic] = existing with { Payload = value };
            }
            else
            {
                if (_pendingMessages.Count == _settings.OutboundCapacity)
                {
                    var droppedTopic = _pendingTopics.First!.Value;
                    _pendingTopics.RemoveFirst();
                    _pendingMessages.Remove(droppedTopic);
                    _droppedCount++;
                    _logger.LogWarning("Dropping oldest MQTT telemetry message because the {Capacity}-item outbound buffer is full", _settings.OutboundCapacity);
                }

                var node = _pendingTopics.AddLast(fullTopic);
                _pendingMessages.Add(fullTopic, new PendingMqttMessage(fullTopic, value, node));
                _bufferedCount = _pendingMessages.Count;
            }
        }

        SignalWorker();
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        await base.StartAsync(cancellationToken).ConfigureAwait(false);
        if (!_settings.Required)
        {
            return;
        }

        try
        {
            await _initialConnection.Task.WaitAsync(
                TimeSpan.FromSeconds(_settings.RequiredInitialConnectionTimeoutSeconds),
                cancellationToken).ConfigureAwait(false);
        }
        catch (TimeoutException exception)
        {
            throw new InvalidOperationException(
                $"MQTT broker did not connect within {_settings.RequiredInitialConnectionTimeoutSeconds} seconds.",
                exception);
        }
    }

    /// <inheritdoc />
    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_connection.IsConnected)
        {
            try
            {
                await _connection.DisconnectAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // The host shutdown deadline elapsed before MQTT could acknowledge disconnect.
            }
            catch (Exception exception)
            {
                _logger.LogDebug(exception, "MQTT disconnect failed during shutdown");
            }
        }

        await base.StopAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var attempt = 0;
        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                if (!_connection.IsConnected)
                {
                    try
                    {
                        await _connection.ConnectAsync(_options, stoppingToken).ConfigureAwait(false);
                        attempt = 0;
                        lock (_sync)
                        {
                            _isConnected = true;
                            _lastSuccessfulConnection = DateTimeOffset.UtcNow;
                        }

                        _initialConnection.TrySetResult();
                        _logger.LogInformation("MQTT client connected");
                    }
                    catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                    {
                        break;
                    }
                    catch (Exception exception)
                    {
                        attempt++;
                        lock (_sync)
                        {
                            _isConnected = false;
                            _retryCount++;
                        }

                        var delay = GetRetryDelay(attempt);
                        _logger.LogWarning(exception, "MQTT connection attempt {Attempt} failed; retrying in {RetryDelay}", attempt, delay);
                        await Task.Delay(delay, stoppingToken).ConfigureAwait(false);
                        continue;
                    }
                }

                try
                {
                    await FlushBufferedMessagesAsync(stoppingToken).ConfigureAwait(false);
                    await WaitForSignalOrConnectionCheckAsync(stoppingToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception exception)
                {
                    lock (_sync)
                    {
                        _isConnected = false;
                        _retryCount++;
                    }

                    var delay = GetRetryDelay(++attempt);
                    _logger.LogWarning(exception, "MQTT publish attempt {Attempt} failed; retrying in {RetryDelay}", attempt, delay);
                    await Task.Delay(delay, stoppingToken).ConfigureAwait(false);
                }
            }
        }
        finally
        {
            lock (_sync)
            {
                _isConnected = false;
            }

            _initialConnection.TrySetCanceled(stoppingToken);
        }
    }

    /// <inheritdoc />
    public override void Dispose()
    {
        _connection.Dispose();
        _signal.Dispose();
        base.Dispose();
    }

    private async Task FlushBufferedMessagesAsync(CancellationToken cancellationToken)
    {
        while (TryGetNextMessage(out var message))
        {
            var mqttMessage = new MqttApplicationMessageBuilder()
                .WithTopic(message.Topic)
                .WithPayload(message.Payload)
                .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.AtMostOnce)
                .WithRetainFlag(false)
                .Build();
            await _connection.PublishAsync(mqttMessage, cancellationToken).ConfigureAwait(false);
            RemoveMessage(message.Topic);
        }
    }

    private bool TryGetNextMessage(out PendingMqttMessage message)
    {
        lock (_sync)
        {
            if (_pendingTopics.First is null)
            {
                message = default!;
                return false;
            }

            message = _pendingMessages[_pendingTopics.First.Value];
            return true;
        }
    }

    private void RemoveMessage(string topic)
    {
        lock (_sync)
        {
            if (_pendingMessages.Remove(topic, out var message))
            {
                _pendingTopics.Remove(message.Node);
                _bufferedCount = _pendingMessages.Count;
            }
        }
    }

    private async Task WaitForSignalOrConnectionCheckAsync(CancellationToken cancellationToken)
    {
        var signalTask = _signal.WaitAsync(cancellationToken);
        var connectionCheckTask = Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
        await Task.WhenAny(signalTask, connectionCheckTask).ConfigureAwait(false);
        if (!_connection.IsConnected)
        {
            lock (_sync)
            {
                _isConnected = false;
            }
        }
    }

    private TimeSpan GetRetryDelay(int attempt)
    {
        var exponentialSeconds = Math.Min(
            _settings.RetryMaxDelaySeconds,
            _settings.RetryMinDelaySeconds * Math.Pow(2, Math.Min(attempt - 1, 30)));
        var jitter = Random.Shared.NextDouble() * Math.Min(1, exponentialSeconds * 0.2);
        return TimeSpan.FromSeconds(exponentialSeconds + jitter);
    }

    private void SignalWorker()
    {
        try
        {
            _signal.Release();
        }
        catch (SemaphoreFullException)
        {
            // A queued signal already guarantees that the worker will observe this message.
        }
    }

    private sealed record PendingMqttMessage(string Topic, string Payload, LinkedListNode<string> Node);
}
