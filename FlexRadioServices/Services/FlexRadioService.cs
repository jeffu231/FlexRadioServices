using System.Collections.ObjectModel;
using System.ComponentModel;
using Flex.Smoothlake.FlexLib;
using Flex.UiWpfFramework.Mvvm;
using FlexRadioServices.Models;
using FlexRadioServices.Models.Settings;

namespace FlexRadioServices.Services
{
    public class FlexRadioService: ObservableObject, IFlexRadioService
    {
        private readonly ILogger _logger;
        private readonly Object _radioListLockObject = new object();
        private RadioProxy? _connectedRadio;
        private string _preferredRadio;

        public FlexRadioService(ILogger<FlexRadioService> logger)
        {
            _logger = logger;
            DiscoveredRadios = new ObservableCollection<RadioProxy>();
            _preferredRadio = AppSettings.RadioSettings.PreferredRadioIdentifier ?? string.Empty;
            foreach (var radio in API.RadioList)
            {
                OnRadioAdded(radio);
            }
            API.RadioAdded +=  OnRadioAdded;
            API.RadioRemoved += OnRadioRemoved;
        }
        
        private void OnRadioAdded(Radio radio)
        {
            _logger.LogDebug("Radio added {RadioNickname}:{RadioSerial}", radio.Nickname, radio.Serial);

            lock (_radioListLockObject)
            {
                bool newRadio = DiscoveredRadios.FirstOrDefault(r => r.Serial == radio.Serial) == null;
                if (newRadio)
                {
                    var radioProxy = new RadioProxy(radio);
                    radioProxy.PropertyChanged += RadioOnPropertyChanged;
                    DiscoveredRadios.Add(radioProxy);
                    if (radioProxy.Serial == _preferredRadio && AppSettings.RadioSettings.AutoConnect)
                    {
                        radioProxy.Radio.Connect();
                    }
                }
                
            }
        }

        private void RadioOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Radio.Connected) && sender is RadioProxy radioProxy)
            {
                if (radioProxy.Connected && radioProxy != ConnectedRadio)
                {
                    _logger.LogDebug("Radio connected");
                    ConnectedRadio = radioProxy;
                    var client = radioProxy.Radio.GuiClients.FirstOrDefault();
                    if (client != null)
                    {
                        _logger.LogDebug("Binding to client {Client}", client.Program);
                        radioProxy.Radio.BoundClientID = client.ClientID;
                    }
                }
                else if (!radioProxy.Connected && radioProxy == ConnectedRadio)
                {
                    _logger.LogDebug("Radio disconnected");
                    ConnectedRadio = null;
                }
            }
        }

        private void OnRadioRemoved(Radio radio)
        {
            _logger.LogDebug("Radio removed {RadioNickname}:{RadioSerial}", radio.Nickname, radio.Serial);

            lock (_radioListLockObject)
            {
                var radioProxy = DiscoveredRadios.FirstOrDefault(r => r.Serial == radio.Serial);
                if (radioProxy != null)
                {
                    DiscoveredRadios.Remove(radioProxy);
                    if (radioProxy == ConnectedRadio)
                    {
                        ConnectedRadio = null;
                    }
                }
            }
        }
        
        public ObservableCollection<RadioProxy> DiscoveredRadios { get; set; }

        public RadioProxy? ConnectedRadio
        {
            get => _connectedRadio;
            set
            {
                if (_connectedRadio == value) return;
                _connectedRadio = value;
                RaisePropertyChanged(nameof(ConnectedRadio));
            }
        }

        public void DisconnectSession()
        {
            API.CloseSession();
        }
        
        public void ConnectToRadio(RadioProxy radio)
        {
            if (radio.Connected) return;
            
            radio.Radio.Connect();
        
            if (!radio.Connected)
            {
                _logger.LogError("Couldn't connect to Radio");
                return;
            }
            
            _logger.LogDebug("Connected!");
            
            // Clients.Clear();
            //
            // var clients = radio.GuiClients.Select(gc =>
            //     new RadioClient(gc.ClientID, gc.ClientHandle, gc.Station, gc.Program));
            // foreach (var client in clients)
            // {
            //     Clients.Add(client);
            // }
        }
        
        public void DisconnectRadio(RadioProxy radio)
        {
            radio.Radio.Disconnect();
            
            _logger.LogDebug("Disconnected");
            
            //Clients.Clear();
        }
    }
}