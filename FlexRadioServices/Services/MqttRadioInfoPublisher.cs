using System.Collections.Concurrent;
using System.ComponentModel;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Channels;
using Flex.Smoothlake.FlexLib;
using FlexRadioServices.Utils;

namespace FlexRadioServices.Services;

/// <summary>
/// Publishes radio state and meter updates through a supervised MQTT work stream.
/// </summary>
internal sealed class MqttRadioInfoPublisher : ConnectedRadioServiceBase, IMqttRadioInfoPublisher
{
    private const int StateQueueCapacity = 1024;
    private readonly ConcurrentDictionary<string, OutgoingMqttMessage> _meterMessages = new();
    private readonly SemaphoreSlim _meterSignal = new(0, 1);
    private readonly IMqttClientService _mqttClientService;
    private readonly ILogger<MqttRadioInfoPublisher> _logger;
    private readonly Channel<OutgoingMqttMessage> _stateMessages = Channel.CreateBounded<OutgoingMqttMessage>(
        new BoundedChannelOptions(StateQueueCapacity)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = true,
            SingleWriter = false
        });
    private int _acceptingMessages = 1;
    private int _meterSignalPending;
    private long _coalescedMeterMessages;
    private long _droppedStateMessages;
    private float _fanLastValue;
    private float _fwdPwrLastValue;
    private float _paTempLastValue;
    private float _refPwrLastValue;
    private float _swrLastValue;
    private float _voltsLastValue;

    /// <summary>
    /// Initializes a new instance of the <see cref="MqttRadioInfoPublisher" /> class.
    /// </summary>
    /// <param name="logger">The logger used to report publishing activity and failures.</param>
    /// <param name="connectedRadioCoordinator">The coordinator that supplies connected-radio transitions.</param>
    /// <param name="mqttClientService">The MQTT client used to publish queued messages.</param>
    public MqttRadioInfoPublisher(
        ILogger<MqttRadioInfoPublisher> logger,
        IConnectedRadioCoordinator connectedRadioCoordinator,
        IMqttClientService mqttClientService)
        : base(connectedRadioCoordinator, logger)
    {
        _logger = logger;
        _mqttClientService = mqttClientService;
    }

    internal long CoalescedMeterMessages => Interlocked.Read(ref _coalescedMeterMessages);

    internal long DroppedStateMessages => Interlocked.Read(ref _droppedStateMessages);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var stateConsumer = ConsumeStateMessagesAsync(stoppingToken);
        var meterConsumer = ConsumeMeterMessagesAsync(stoppingToken);

        try
        {
            await base.ExecuteAsync(stoppingToken).ConfigureAwait(false);
        }
        finally
        {
            Interlocked.Exchange(ref _acceptingMessages, 0);
            _stateMessages.Writer.TryComplete();
            TrySignalMeterConsumer();
            await Task.WhenAll(stateConsumer, meterConsumer).ConfigureAwait(false);
        }
    }

    protected override void ConnectedRadioChanged(object? sender, ConnectedRadioEventArgs args)
    {
        var previousRadio = args.PreviousRadio?.Radio;
        if (previousRadio is not null)
        {
            foreach (var slice in previousRadio.SliceList)
            {
                RadioOnSliceRemoved(slice);
            }

            previousRadio.SliceAdded -= RadioOnSliceAdded;
            previousRadio.SliceRemoved -= RadioOnSliceRemoved;
            RemoveRadioMeterListeners(previousRadio);
        }

        if (ConnectedRadio is null)
        {
            return;
        }

        foreach (var slice in ConnectedRadio.Radio.SliceList)
        {
            RadioOnSliceAdded(slice);
        }

        ConnectedRadio.Radio.SliceAdded += RadioOnSliceAdded;
        ConnectedRadio.Radio.SliceRemoved += RadioOnSliceRemoved;
        AddRadioMeterListeners(ConnectedRadio.Radio);
    }

    protected override void RadioOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is Radio radio && e.PropertyName == nameof(Radio.InterlockState))
        {
            HandleMoxChange(radio);
        }
    }

    private async Task ConsumeStateMessagesAsync(CancellationToken cancellationToken)
    {
        await foreach (var message in _stateMessages.Reader.ReadAllAsync().ConfigureAwait(false))
        {
            await PublishMessageAsync(message, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task ConsumeMeterMessagesAsync(CancellationToken cancellationToken)
    {
        while (Volatile.Read(ref _acceptingMessages) == 1 || !_meterMessages.IsEmpty)
        {
            await _meterSignal.WaitAsync().ConfigureAwait(false);

            foreach (var meterMessage in _meterMessages.ToArray())
            {
                if (_meterMessages.TryRemove(meterMessage.Key, out var latestMessage))
                {
                    await PublishMessageAsync(latestMessage, cancellationToken).ConfigureAwait(false);
                }
            }

            Interlocked.Exchange(ref _meterSignalPending, 0);
            if (!_meterMessages.IsEmpty)
            {
                TrySignalMeterConsumer();
            }
        }
    }

    private async Task PublishMessageAsync(OutgoingMqttMessage message, CancellationToken cancellationToken)
    {
        try
        {
            await _mqttClientService.PublishAsync(message.Topic, message.Payload, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // The host's shutdown deadline has elapsed; do not begin another publish.
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Unable to publish MQTT {Kind} message for topic {Topic}", message.Kind, message.Topic);
        }
    }

    private void HandleMoxChange(Radio radio)
    {
        try
        {
            var serial = radio.Serial;
            var interlockState = radio.InterlockState;
            var isMox = RadioManagerService.IsInterlockMox(interlockState);
            _logger.LogDebug("Interlock changed {InterlockState}", interlockState);
            EnqueueStateMessage($"radios/{serial}/mox", isMox.ToString().ToLower(CultureInfo.InvariantCulture));

            if (!isMox)
            {
                _logger.LogDebug("Interlock MOX changed to false");
                return;
            }

            _logger.LogDebug("Interlock MOX changed to true");
            var transmitSlice = radio.SliceList.ToArray()
                .FirstOrDefault(slice => slice.IsTransmitSlice && slice.ClientHandle == radio.TXClientHandle);
            if (transmitSlice is not null)
            {
                EnqueueRadioTxBandInfo(transmitSlice);
            }
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Skipping MOX event because radio data is unavailable");
        }
    }

    private void RadioOnSliceRemoved(Slice slice)
    {
        _logger.LogDebug("Removed slice {Letter} listener for radio {RadioSerial}", slice.Letter, slice.Radio.Serial);
        slice.PropertyChanged -= SliceOnPropertyChanged;
    }

    private void RadioOnSliceAdded(Slice slice)
    {
        _logger.LogDebug("Added slice {Letter} listener for radio {RadioSerial}", slice.Letter, slice.Radio.Serial);
        slice.PropertyChanged += SliceOnPropertyChanged;
        PublishInitialSliceInfo(slice);
    }

    private void SliceOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is not Slice slice || e.PropertyName is null)
        {
            return;
        }

        try
        {
            var guiClient = slice.Radio.FindGUIClientByClientHandle(slice.ClientHandle);
            if (guiClient is null)
            {
                _logger.LogDebug("Skipping slice {Letter} property {Property}; GUI client is unavailable", slice.Letter, e.PropertyName);
                return;
            }

            var propertyName = JsonNamingPolicy.CamelCase.ConvertName(e.PropertyName);
            _logger.LogDebug("Property name {Property} changed", e.PropertyName);
            EnqueueStateMessage(
                $"radios/{slice.Radio.Serial}/client/{guiClient.ClientID}/slice/{slice.Letter}/{propertyName}",
                GetPropValueAsString(slice, e.PropertyName));

            if ((slice.IsTransmitSlice && e.PropertyName is nameof(Slice.TXAnt) or nameof(Slice.Freq)) ||
                (e.PropertyName == nameof(Slice.IsTransmitSlice) && slice.IsTransmitSlice))
            {
                EnqueueClientTxBandInfo(slice, guiClient.ClientID);
            }
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Skipping slice property {Property} because its data is unavailable", e.PropertyName);
        }
    }

    private void PublishInitialSliceInfo(Slice slice)
    {
        try
        {
            if (!slice.IsTransmitSlice)
            {
                return;
            }

            var guiClient = slice.Radio.FindGUIClientByClientHandle(slice.ClientHandle);
            if (guiClient is null)
            {
                _logger.LogDebug("Skipping initial TX info for slice {Letter}; GUI client is unavailable", slice.Letter);
                return;
            }

            EnqueueClientTxBandInfo(slice, guiClient.ClientID);
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Skipping initial TX info because slice data is unavailable");
        }
    }

    private void EnqueueRadioTxBandInfo(Slice slice)
    {
        try
        {
            var radio = slice.Radio;
            var serial = radio.Serial;
            var letter = slice.Letter;
            var clientHandle = slice.ClientHandle;
            var guiClient = radio.FindGUIClientByClientHandle(clientHandle);
            if (guiClient is null)
            {
                _logger.LogDebug("Skipping radio TX info for serial {RadioSerial}, slice {Letter}, client handle {ClientHandle}; GUI client is unavailable",
                    serial, letter, clientHandle);
                return;
            }

            var clientId = guiClient.ClientID;
            _logger.LogDebug("Publishing TX BAND info for radio {RadioSerial}", serial);
            EnqueueStateMessage($"radios/{serial}/tx_info", GetTxSliceInfoJson(slice, clientId));
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Skipping radio TX info because slice data is unavailable");
        }
    }

    private void EnqueueClientTxBandInfo(Slice slice, string clientId)
    {
        _logger.LogDebug("Publishing TX BAND info for radio {RadioSerial} / Client {GuiClient}", slice.Letter, clientId);
        EnqueueStateMessage($"radios/{slice.Radio.Serial}/client/{clientId}/tx_info", GetTxSliceInfoJson(slice));
    }

    private void EnqueueStateMessage(string topic, string payload)
    {
        if (Volatile.Read(ref _acceptingMessages) == 0)
        {
            return;
        }

        if (!_stateMessages.Writer.TryWrite(new OutgoingMqttMessage(topic, payload, MqttMessageKind.State)))
        {
            Interlocked.Increment(ref _droppedStateMessages);
            _logger.LogWarning("Dropping MQTT state message because the {Capacity}-item queue is full for topic {Topic}", StateQueueCapacity, topic);
        }
    }

    private void EnqueueMeterMessage(string meterName, double data)
    {
        var radioSerial = ConnectedRadio?.Radio.Serial;
        if (radioSerial is null || Volatile.Read(ref _acceptingMessages) == 0)
        {
            return;
        }

        var message = new OutgoingMqttMessage(
            $"radios/{radioSerial}/meters/{meterName}",
            data.ToString(CultureInfo.InvariantCulture),
            MqttMessageKind.Meter);
        if (_meterMessages.TryGetValue(message.Topic, out _))
        {
            Interlocked.Increment(ref _coalescedMeterMessages);
        }

        _meterMessages[message.Topic] = message;
        TrySignalMeterConsumer();
    }

    private void TrySignalMeterConsumer()
    {
        if (Interlocked.Exchange(ref _meterSignalPending, 1) == 0)
        {
            _meterSignal.Release();
        }
    }

    private static string GetTxSliceInfoJson(Slice slice, string? clientId = null)
    {
        var payload = new
        {
            slice = slice.Letter,
            txAnt = slice.TXAnt,
            freq = slice.Freq,
            band = BandConverter.ConvertToBand(slice.Freq * 1000),
            clientID = clientId
        };
        var options = new JsonSerializerOptions { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull };
        return JsonSerializer.Serialize(payload, options);
    }

    private static string GetPropValueAsString(object source, string propertyName)
        => Convert.ToString(source.GetType().GetProperty(propertyName)?.GetValue(source), CultureInfo.InvariantCulture) ?? string.Empty;

    private void AddRadioMeterListeners(Radio radio)
    {
        radio.VoltsDataReady += RadioOnVoltsDataReady;
        radio.PATempDataReady += RadioOnPATempDataReady;
        radio.ForwardPowerDataReady += RadioOnForwardPowerDataReady;
        radio.ReflectedPowerDataReady += RadioOnReflectedPowerDataReady;
        radio.SWRDataReady += RadioOnSWRDataReady;
        radio.MainFanDataReady += RadioOnMainFanDataReady;
    }

    private void RemoveRadioMeterListeners(Radio radio)
    {
        radio.VoltsDataReady -= RadioOnVoltsDataReady;
        radio.PATempDataReady -= RadioOnPATempDataReady;
        radio.ForwardPowerDataReady -= RadioOnForwardPowerDataReady;
        radio.ReflectedPowerDataReady -= RadioOnReflectedPowerDataReady;
        radio.SWRDataReady -= RadioOnSWRDataReady;
        radio.MainFanDataReady -= RadioOnMainFanDataReady;
    }

    private void RadioOnMainFanDataReady(float data)
    {
        if (Math.Abs(data - _fanLastValue) > .01f)
        {
            _fanLastValue = data;
            EnqueueMeterMessage("main_fan", data);
        }
    }

    private void RadioOnSWRDataReady(float data)
    {
        if (Math.Abs(data - _swrLastValue) > .01f)
        {
            _swrLastValue = data;
            EnqueueMeterMessage("swr", data);
        }
    }

    private void RadioOnReflectedPowerDataReady(float data)
    {
        if (Math.Abs(data - _refPwrLastValue) > .001f)
        {
            _refPwrLastValue = data;
            EnqueueMeterMessage("ref_pwr", ConvertDbmToWatts(data));
        }
    }

    private void RadioOnForwardPowerDataReady(float data)
    {
        if (Math.Abs(data - _fwdPwrLastValue) > .001f)
        {
            _fwdPwrLastValue = data;
            EnqueueMeterMessage("fwd_pwr", ConvertDbmToWatts(data));
        }
    }

    private void RadioOnPATempDataReady(float data)
    {
        if (Math.Abs(data - _paTempLastValue) > .01f)
        {
            _paTempLastValue = data;
            EnqueueMeterMessage("pa_temp", data);
        }
    }

    private void RadioOnVoltsDataReady(float data)
    {
        if (Math.Abs(data - _voltsLastValue) > .01f)
        {
            _voltsLastValue = data;
            EnqueueMeterMessage("voltage", data);
        }
    }

    private static double ConvertDbmToWatts(float dbm) => dbm == 0 ? 0 : Math.Pow(10, dbm / 10) / 1000;
}
