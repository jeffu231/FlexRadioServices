using System.Collections.Immutable;
using System.ComponentModel;
using Flex.Smoothlake.FlexLib;
using FlexRadioServices.Models;
using FlexRadioServices.Models.Radio;
using FlexRadioServices.Models.Settings;
using FlexRadioServices.Services.FlexLib;
using Microsoft.Extensions.Options;

namespace FlexRadioServices.Services;

/// <summary>
/// Coordinates FlexLib radio discovery and publishes copied application state.
/// </summary>
public sealed class FlexRadioService : IFlexRadioService, IConnectedRadioCoordinator
{
    private readonly ILogger _logger;
    private readonly IFlexLibApi _flexLibApi;
    private readonly object _radioLock = new();
    private readonly Dictionary<string, RadioProxy> _discoveredRadios = new(StringComparer.Ordinal);
    private readonly string _preferredRadio;
    private readonly IOptions<RadioSettings> _radioSettings;
    private RadioProxy? _connectedRadio;
    private bool _initialized;

    internal event EventHandler<ConnectedRadioTransition>? ConnectedRadioChanged;

    event EventHandler<ConnectedRadioTransition>? IConnectedRadioCoordinator.ConnectedRadioChanged
    {
        add => ConnectedRadioChanged += value;
        remove => ConnectedRadioChanged -= value;
    }

    public FlexRadioService(ILogger<FlexRadioService> logger, IOptions<RadioSettings> radioSettings, IFlexLibApi flexLibApi)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(radioSettings);
        ArgumentNullException.ThrowIfNull(flexLibApi);
        _logger = logger;
        _radioSettings = radioSettings;
        _flexLibApi = flexLibApi;
        _preferredRadio = radioSettings.Value.PreferredRadioIdentifier ?? string.Empty;
    }

    internal void Initialize()
    {
        lock (_radioLock)
        {
            if (_initialized)
            {
                return;
            }

            _flexLibApi.RadioAdded += OnRadioAdded;
            _flexLibApi.RadioRemoved += OnRadioRemoved;
            _initialized = true;
            foreach (var radio in _flexLibApi.Radios)
            {
                AddRadioLocked(radio);
            }
        }
    }

    internal void Stop()
    {
        EventHandler<ConnectedRadioTransition>? changed;
        ConnectedRadioTransition? transition;
        lock (_radioLock)
        {
            if (!_initialized)
            {
                return;
            }

            _flexLibApi.RadioAdded -= OnRadioAdded;
            _flexLibApi.RadioRemoved -= OnRadioRemoved;
            _initialized = false;
            foreach (var proxy in _discoveredRadios.Values)
            {
                proxy.PropertyChanged -= RadioOnPropertyChanged;
            }

            _discoveredRadios.Clear();
            var previousRadio = _connectedRadio;
            changed = previousRadio is null ? null : ConnectedRadioChanged;
            _connectedRadio = null;
            transition = previousRadio is null ? null : new ConnectedRadioTransition(previousRadio, null);
        }

        if (transition is not null)
        {
            changed?.Invoke(this, transition);
        }
    }

    public ImmutableArray<RadioSnapshot> GetDiscoveredRadios()
    {
        lock (_radioLock)
        {
            return _discoveredRadios.Values.Select(CreateSnapshot).ToImmutableArray();
        }
    }

    public RadioSnapshot? GetConnectedRadio()
    {
        lock (_radioLock)
        {
            return _connectedRadio is null ? null : CreateSnapshot(_connectedRadio);
        }
    }

    public ImmutableArray<RadioClientSnapshot> GetRadioClients(string serial)
    {
        lock (_radioLock)
        {
            return _discoveredRadios.TryGetValue(serial, out var radio)
                ? radio.Radio.GuiClients.Select(CreateSnapshot).ToImmutableArray()
                : [];
        }
    }

    public bool ConnectToRadio(string serial)
    {
        lock (_radioLock)
        {
            if (!_discoveredRadios.TryGetValue(serial, out var radio)) return false;
            if (!radio.Connected) radio.Radio.Connect();
            return true;
        }
    }

    public bool DisconnectRadio(string serial)
    {
        lock (_radioLock)
        {
            if (!_discoveredRadios.TryGetValue(serial, out var radio)) return false;
            if (radio.Connected) radio.Radio.Disconnect();
            return true;
        }
    }

    internal RadioProxy? GetConnectedRadioHandle()
    {
        lock (_radioLock)
        {
            return _connectedRadio;
        }
    }

    RadioProxy? IConnectedRadioCoordinator.GetConnectedRadioHandle() => GetConnectedRadioHandle();

    internal RadioProxy? GetRadioHandle(string serial)
    {
        lock (_radioLock)
        {
            return _discoveredRadios.GetValueOrDefault(serial);
        }
    }

    private void OnRadioAdded(Radio radio)
    {
        _logger.LogDebug("Radio added {RadioNickname}:{RadioSerial}", radio.Nickname, radio.Serial);
        lock (_radioLock)
        {
            AddRadioLocked(radio);
        }
    }

    private void AddRadioLocked(Radio radio)
    {
        var serial = radio.Serial ?? string.Empty;
        if (_discoveredRadios.ContainsKey(serial)) return;

        var proxy = new RadioProxy(radio);
        proxy.PropertyChanged += RadioOnPropertyChanged;
        _discoveredRadios.Add(serial, proxy);
        if (serial == _preferredRadio && _radioSettings.Value.AutoConnect)
        {
            radio.Connect();
        }
    }

    private void RadioOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is not RadioProxy radio || e.PropertyName != nameof(Radio.Connected)) return;

        EventHandler<ConnectedRadioTransition>? changed = null;
        ConnectedRadioTransition? transition = null;
        lock (_radioLock)
        {
            if (radio.Connected && !ReferenceEquals(radio, _connectedRadio))
            {
                var previousRadio = _connectedRadio;
                _connectedRadio = radio;
                var client = radio.Radio.GuiClients.FirstOrDefault();
                if (client is not null) radio.Radio.BoundClientID = client.ClientID;
                changed = ConnectedRadioChanged;
                transition = new ConnectedRadioTransition(previousRadio, radio);
            }
            else if (!radio.Connected && ReferenceEquals(radio, _connectedRadio))
            {
                var previousRadio = _connectedRadio;
                _connectedRadio = null;
                changed = ConnectedRadioChanged;
                transition = new ConnectedRadioTransition(previousRadio, null);
            }
        }

        if (transition is not null)
        {
            changed?.Invoke(this, transition);
        }
    }

    private void OnRadioRemoved(Radio radio)
    {
        _logger.LogDebug("Radio removed {RadioNickname}:{RadioSerial}", radio.Nickname, radio.Serial);
        EventHandler<ConnectedRadioTransition>? changed = null;
        ConnectedRadioTransition? transition = null;
        lock (_radioLock)
        {
            var serial = radio.Serial ?? string.Empty;
            if (!_discoveredRadios.Remove(serial, out var proxy)) return;
            proxy.PropertyChanged -= RadioOnPropertyChanged;
            if (ReferenceEquals(proxy, _connectedRadio))
            {
                var previousRadio = _connectedRadio;
                _connectedRadio = null;
                changed = ConnectedRadioChanged;
                transition = new ConnectedRadioTransition(previousRadio, null);
            }
        }

        if (transition is not null)
        {
            changed?.Invoke(this, transition);
        }
    }

    private static RadioSnapshot CreateSnapshot(RadioProxy radio) => new(
        radio.Ip, radio.BranchName, radio.Model, radio.Nickname, radio.Callsign,
        radio.Serial, radio.Version, radio.Connected, radio.ConnectedState,
        radio.Status, radio.CommandPort, radio.IsWan, radio.BoundClientId,
        radio.ClientHandle, radio.GuiClientId, radio.TransmitSlice, radio.TxClientHandle);

    private static RadioClientSnapshot CreateSnapshot(GUIClient client) => new(
        client.ClientID, client.ClientHandle, client.Station, client.Program,
        client.IsLocalPtt, client.TransmitSlice?.Letter);
}
