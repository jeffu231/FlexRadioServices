using System.ComponentModel;
using System.Globalization;
using Flex.Smoothlake.FlexLib;

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
        }

        if (ConnectedRadio != null)
        {
            foreach (var slice in ConnectedRadio.Radio.SliceList)
            {
                RadioOnSliceAdded(slice);
            }
            ConnectedRadio.Radio.SliceAdded += RadioOnSliceAdded;
            ConnectedRadio.Radio.SliceRemoved += RadioOnSliceRemoved;
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
            await _mqttClientService.Publish($"radios/{slice.Radio.Serial}/slice/{slice.Letter}/{prop}", 
                GetPropValue(slice, e.PropertyName).ToString() ?? string.Empty);
        }
    }

    private static object GetPropValue(object src, string propName)
    {
        return src.GetType().GetProperty(propName)?.GetValue(src, null)??string.Empty;
    }
    
}
