using System.ComponentModel;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Flex.Smoothlake.FlexLib;
using FlexRadioServices.Utils;

namespace FlexRadioServices.Services;

public sealed class MqttRadioInfoPublisher:ConnectedRadioServiceBase, IMqttRadioInfoPublisher
{
    private readonly IMqttClientService _mqttClientService;
    private readonly ILogger<MqttRadioInfoPublisher> _logger;
    
    public MqttRadioInfoPublisher(ILogger<MqttRadioInfoPublisher> logger, IFlexRadioService flexRadioService, 
        IMqttClientService mqttClientService):base(flexRadioService, logger)
    {
        _logger = logger;
        _mqttClientService = mqttClientService;
    }

    protected override void ConnectedRadioChanged(object? sender, ConnectedRadioEventArgs args)
    {
        if (args.PreviousRadio != null)
        {
            foreach (var slice in args.PreviousRadio.Radio.SliceList)
            {
                RadioOnSliceRemoved(slice);
            }
            args.PreviousRadio.Radio.SliceAdded -= RadioOnSliceAdded;
            args.PreviousRadio.Radio.SliceRemoved -= RadioOnSliceRemoved;
            RemoveRadioMeterListeners(args.PreviousRadio.Radio);
        }

        if (ConnectedRadio != null)
        {
            foreach (var slice in ConnectedRadio.Radio.SliceList)
            {
                RadioOnSliceAdded(slice);
            }
            ConnectedRadio.Radio.SliceAdded += RadioOnSliceAdded;
            ConnectedRadio.Radio.SliceRemoved += RadioOnSliceRemoved;
            AddRadioMeterListeners(ConnectedRadio.Radio);
        }
    }


    protected override async void RadioOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is Radio r)
        {
            //Interlock states will occur before the Mox change event fires.
            if (e.PropertyName == nameof(Radio.InterlockState))
            {
                _logger.LogInformation("Interlock changed {InterlockState}", r.InterlockState);
                await HandleMoxChange(r);
            }
        }
    }
    
    private async Task HandleMoxChange(Radio r)
    {
        await PublishMoxState(r);
        if (RadioManagerService.IsInterlockMox(r.InterlockState))
        {
            _logger.LogDebug("Interlock MOX changed to true");
            var txSlice = r.SliceList.ToArray()
                .FirstOrDefault(s => s.IsTransmitSlice && s.ClientHandle == r.TXClientHandle);
            if (txSlice is not null)
            {
                await PublishRadioTxBandInfo(txSlice);
            }
            return;
        }
        
        _logger.LogDebug("Interlock MOX changed to false");
        await ClearRadioTxBandInfo(r);
        
    }

    private void RadioOnSliceRemoved(Slice slc)
    {
        _logger.LogDebug("Removed slice {Letter} listener for radio {RadioSerial}",slc.Letter, slc.Radio.Serial);
        slc.PropertyChanged -= SliceOnPropertyChanged;
    }

    private void RadioOnSliceAdded(Slice slc)
    {
        _logger.LogDebug("Added slice {Letter} listener for radio {RadioSerial}",slc.Letter, slc.Radio.Serial);
        slc.PropertyChanged += SliceOnPropertyChanged;
        PublishInitialSliceInfo(slc);
    }

    private async void SliceOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is Slice slice && e.PropertyName != null)
        {
            try
            {
                var prop = System.Text.Json.JsonNamingPolicy.CamelCase.ConvertName(e.PropertyName);
                _logger.LogDebug("Property name {EPropertyName} changed", e.PropertyName);
                var guiClient = slice.Radio.FindGUIClientByClientHandle(slice.ClientHandle);
                await _mqttClientService.Publish(
                    $"radios/{slice.Radio.Serial}/client/{guiClient.ClientID}/slice/{slice.Letter}/{prop}",
                    GetPropValue(slice, e.PropertyName).ToString() ?? string.Empty);
                
                if (slice.IsTransmitSlice && e.PropertyName is nameof(Slice.TXAnt) or nameof(Slice.Freq)
                    || e.PropertyName is nameof(Slice.IsTransmitSlice) && slice.IsTransmitSlice)
                {
                    await PublishClientTxBandInfo(slice);
                }
            }
            catch (Exception ex)
            {
                //Sometimes this can happen if a slice goes away in the middle of a change
                //Just log it for now to contain it and determine if some variants occur that are not expected.
                _logger.LogError(ex, "Error processing Slice Property {EPropertyName} changed", e.PropertyName);
            }
            
        }
    }
    
    private void PublishInitialSliceInfo(Slice slc)
    {
        if (slc.IsTransmitSlice)
        {
            _ = PublishClientTxBandInfo(slc);
        }
    }

    private async Task PublishMoxState(Radio radio)
    {
        await _mqttClientService.Publish($"radios/{radio.Serial}/mox", 
            RadioManagerService.IsInterlockMox(radio.InterlockState).ToString());
    }

    private async Task ClearRadioTxBandInfo(Radio radio)
    {
        await _mqttClientService.Publish($"radios/{radio.Serial}/tx_info",string.Empty);
    }
    private async Task PublishRadioTxBandInfo(Slice slice)
    {
        _logger.LogDebug("Publishing TX BAND info for radio {RadioSerial}", slice.Letter);
        
        await _mqttClientService.Publish($"radios/{slice.Radio.Serial}/tx_info",
            GetTxSliceInfoJson(slice, true));
    }
    
    private async Task PublishClientTxBandInfo(Slice slice)
    {
        var guiClient = slice.Radio.FindGUIClientByClientHandle(slice.ClientHandle);
        _logger.LogDebug("Publishing TX BAND info for radio {RadioSerial} / Client {GuiClient}",
            slice.Letter, guiClient.ClientID);
        
        await _mqttClientService.Publish($"radios/{slice.Radio.Serial}/client/{guiClient.ClientID}/tx_info",
            GetTxSliceInfoJson(slice));
    }

    private static string GetTxSliceInfoJson(Slice slice, bool includeClientId = false)
    {
        var guiClient = includeClientId
            ? slice.Radio.FindGUIClientByClientHandle(slice.ClientHandle)
            : null;
        
        var payload = new
        {
            slice = slice.Letter,
            txAnt = slice.TXAnt,
            freq = slice.Freq,
            band = BandConverter.ConvertToBand(slice.Freq * 1000),
            clientID = includeClientId ? guiClient?.ClientID : null
        };
        var options = new JsonSerializerOptions
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
        return JsonSerializer.Serialize(payload, options);
    }
    
    private static object GetPropValue(object src, string propName)
    {
        return src.GetType().GetProperty(propName)?.GetValue(src, null)??string.Empty;
    }

    private void AddRadioMeterListeners(Radio radio)
    {
        radio.VoltsDataReady += RadioOnVoltsDataReady;
        radio.PATempDataReady += RadioOnPATempDataReady;
        radio.ForwardPowerDataReady += RadioOnForwardPowerDataReady;
        radio.ReflectedPowerDataReady += RadioOnReflectedPowerDataReady;
        radio.SWRDataReady += RadioOnSWRDataReady;
        radio.MainFanDataReady += RadioOnMainFanDataReady;
    }

    private float _fanLastValue;
    private async void RadioOnMainFanDataReady(float data)
    {
        if(ConnectedRadio == null) return;
        if (Math.Abs(data - _fanLastValue) > .01f)
        {
            _fanLastValue = data;
            await _mqttClientService.Publish($"radios/{ConnectedRadio.Radio.Serial}/meters/mainfan",
                data.ToString(CultureInfo.InvariantCulture));
        }
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

    private float _swrLastValue;
    private async void RadioOnSWRDataReady(float data)
    {
        if(ConnectedRadio == null) return;
        if (Math.Abs(data - _swrLastValue) > .01f)
        {
            _swrLastValue = data;
            await _mqttClientService.Publish($"radios/{ConnectedRadio.Radio.Serial}/meters/swr",
                data.ToString(CultureInfo.InvariantCulture));
        }
    }

    private float _refPwrLastValue;
    private async void RadioOnReflectedPowerDataReady(float data)
    {
        if(ConnectedRadio == null) return;
        if (Math.Abs(data - _refPwrLastValue) > .001f)
        {
            _refPwrLastValue = data;
            //Meter name = REFPWR
            //Convert dbm to watts
            var w = ConvertDbmToWatts(data);
            await _mqttClientService.Publish($"radios/{ConnectedRadio.Radio.Serial}/meters/ref_pwr",
                w.ToString(CultureInfo.InvariantCulture));
        }
    }

    private float _fwdPwrLastValue;
    private async void RadioOnForwardPowerDataReady(float data)
    {
        if(ConnectedRadio == null) return;
        if (Math.Abs(data - _fwdPwrLastValue) > .001f)
        {
            _fwdPwrLastValue = data;
            //Meter name = FWDPWR
            //Convert dbm to watts
            var w = ConvertDbmToWatts(data);
            await _mqttClientService.Publish($"radios/{ConnectedRadio.Radio.Serial}/meters/fwd_pwr", 
                w.ToString(CultureInfo.InvariantCulture));
        }
    }

    private float _paTempLastValue;
    private async void RadioOnPATempDataReady(float data)
    {
        if(ConnectedRadio == null) return;
        if (Math.Abs(data - _paTempLastValue) > .01f)
        {
            _paTempLastValue = data;
            await _mqttClientService.Publish($"radios/{ConnectedRadio.Radio.Serial}/meters/pa_temp",
                data.ToString(CultureInfo.InvariantCulture));
        }
    }

    private float _voltsLastValue;
    private async void RadioOnVoltsDataReady(float data)
    {
        if(ConnectedRadio == null) return;
        if (Math.Abs(data - _voltsLastValue) > .01f)
        {
            _voltsLastValue = data;
            await _mqttClientService.Publish($"radios/{ConnectedRadio.Radio.Serial}/meters/voltage",
                data.ToString(CultureInfo.InvariantCulture));
        }
    }

    private static double ConvertDbmToWatts(float dbm)
    {
        if (dbm == 0) return 0;
        return Math.Pow(10, dbm / 10) / 1000;
    }
}
