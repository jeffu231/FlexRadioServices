using System.ComponentModel;
using System.Globalization;
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

    protected override async void ConnectedRadioChanged(object? sender, ConnectedRadioEventArgs args)
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
            await AddRadioMeterListeners(ConnectedRadio.Radio);
        }
    }


    protected override void RadioOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        
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
    }
    private async void SliceOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is Slice slice && e.PropertyName != null)
        {  
            var prop = System.Text.Json.JsonNamingPolicy.CamelCase.ConvertName(e.PropertyName);
            _logger.LogInformation("Property name {EPropertyName}", e.PropertyName);
            var guiClient = slice.Radio.FindGUIClientByClientHandle(slice.ClientHandle);
            await _mqttClientService.Publish($"radios/{slice.Radio.Serial}/client/{guiClient.ClientID}/slice/{slice.Letter}/{prop}", 
                GetPropValue(slice, e.PropertyName).ToString() ?? string.Empty);
            
            if (slice.IsTransmitSlice)
            {
                if (slice.IsTransmitSlice)
                {
                    await PublishTxBandInfo(slice);
                }
            }
        }
    }
    private async Task PublishTxBandInfo(Slice slice)
    {
        _logger.LogDebug("Publishing TX BAND info to radio {RadioSerial}", slice.Letter);
        var guiClient = slice.Radio.FindGUIClientByClientHandle(slice.ClientHandle);
        await _mqttClientService.Publish($"radios/{slice.Radio.Serial}/client/{guiClient.ClientID}"+
                                         $"/txslice/{slice.Letter}/txant/{slice.TXAnt}/freq",
            slice.Freq.ToString(CultureInfo.InvariantCulture));
        
        await _mqttClientService.Publish($"radios/{slice.Radio.Serial}/client/{guiClient.ClientID}"+
                                         $"/txslice/{slice.Letter}/txant/{slice.TXAnt}/band",
            BandConverter.ConvertToBand(slice.Freq * 1000).ToString(CultureInfo.InvariantCulture));
    }

    
    private static object GetPropValue(object src, string propName)
    {
        return src.GetType().GetProperty(propName)?.GetValue(src, null)??string.Empty;
    }

    private async Task AddRadioMeterListeners(Radio radio)
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
