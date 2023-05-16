using Flex.Smoothlake.FlexLib;
using FlexRadioServices.Models;

namespace FlexRadioServices.Services
{
    public class FlexLibService: BackgroundService
    {
        private readonly ILogger _logger;
        private readonly ActiveState _activeState;
        private readonly AppSettings _appSettings;

        public FlexLibService(ILogger<FlexLibService> logger, ActiveState activeState, AppSettings appSettings)
        {
            _logger = logger;
            _activeState = activeState;
            _appSettings = appSettings;
        }
        
        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogDebug("Starting Flexlib Service");
            foreach (var radio in API.RadioList)
            {
                OnRadioAdded(radio);
            }
            API.RadioAdded +=  OnRadioAdded;
            API.RadioRemoved += OnRadioRemoved;

            return MonitorLoop(stoppingToken);
        }

        private async Task MonitorLoop(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                await Task.Delay(250, token);
            }
        }
        
        private void OnRadioAdded(Radio radio)
        {
            _logger.LogDebug("Radio added {RadioNickname}:{RadioSerial}", radio.Nickname, radio.Serial);

            var flexRadio = new RadioProxy(radio);
            if(!_activeState.Radios.TryAdd(radio.Serial, flexRadio))
            {
                _activeState.Radios[radio.Serial] = flexRadio;
            }
            
            // if (_activeState.ActiveRadio == null && _appSettings.AutoConnect)
            // {
            //     if (!string.IsNullOrEmpty(_appSettings.PreferredRadioIdentifier)
            //         && (_appSettings.PreferredRadioIdentifier.Equals(radio.Nickname)
            //         || _appSettings.PreferredRadioIdentifier.Equals(radio.Serial)))
            //     {
            //         _activeState.ConnectToRadio(radio);
            //        
            //     }
            //     else
            //     {
            //         //No identifer specified so just connect to the first radio we see.
            //         _activeState.ConnectToRadio(radio);
            //     }
            //     
            //     if (radio.Connected)
            //     {
            //         _logger.LogDebug("Radio connected {RadioNickname}:{RadioSerial}", radio.Nickname, radio.Serial);
            //         _activeState.ActiveRadio = radio;
            //     }
            // }
        }
        
        private void OnRadioRemoved(Radio radio)
        {
            _logger.LogDebug("Radio removed {RadioNickname}:{RadioSerial}", radio.Nickname, radio.Serial);
            
            //_activeState.DisconnectRadio(radio);
            //_activeState.ActiveRadio = null;
            _activeState.Radios.TryRemove(radio.Serial, out _);
            
        }

        
    }
}