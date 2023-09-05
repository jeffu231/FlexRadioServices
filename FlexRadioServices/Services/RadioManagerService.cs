using System.ComponentModel;
using Flex.Smoothlake.FlexLib;

namespace FlexRadioServices.Services;

public class RadioManagerService: ConnectedRadioServiceBase
{

    private ILogger<RadioManagerService> _logger;
    private CancellationTokenSource? _cancellationToken;
    private bool _txSliceMute = false;
    
    public RadioManagerService(IFlexRadioService flexRadioService, ILogger<RadioManagerService> logger) : base(flexRadioService, logger)
    {
        _logger = logger;
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
            args.PreviousRadio.Radio.PanadapterRemoved += RadioOnPanadapterRemoved;
            
            foreach (var panadapter in ConnectedRadio.Radio.PanadapterList.ToList())
            {
                panadapter.PropertyChanged -= PanadapterOnPropertyChanged;
            }
        }

        if (ConnectedRadio != null)
        {
            foreach (var slice in ConnectedRadio.Radio.SliceList)
            {
                RadioOnSliceAdded(slice);
            }

            ConnectedRadio.Radio.SliceAdded += RadioOnSliceAdded;
            ConnectedRadio.Radio.SliceRemoved += RadioOnSliceRemoved;
            ConnectedRadio.Radio.PanadapterAdded += RadioOnPanadapterAdded;
            
            foreach (var panadapter in ConnectedRadio.Radio.PanadapterList.ToList())
            {
                panadapter.PropertyChanged += PanadapterOnPropertyChanged;
            }
        }
    }

    private void RadioOnPanadapterRemoved(Panadapter pan)
    {
        pan.PropertyChanged -= PanadapterOnPropertyChanged;
    }

    private void RadioOnPanadapterAdded(Panadapter pan, Waterfall fall)
    {
        pan.PropertyChanged += PanadapterOnPropertyChanged;
    }

    

    private void RadioOnSliceRemoved(Slice slc)
    {
        _logger.LogDebug("Removed slice {Letter} listener for radio {RadioSerial}", 
            slc.Letter, slc.Radio.Serial);
        slc.PropertyChanged -= SliceOnPropertyChanged;
        
    }

    private void RadioOnSliceAdded(Slice slc)
    {
        _logger.LogDebug("Added slice {Letter} listener for radio {RadioSerial}", 
            slc.Letter, slc.Radio.Serial);
        slc.PropertyChanged += SliceOnPropertyChanged;
    }
    
    private void PanadapterOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is Panadapter p && (e.PropertyName == nameof(Panadapter.Band) || e.PropertyName == nameof(Panadapter.XVTR)))
        
        {
            _logger.LogInformation("Panadapter property {Prop} changed", 
                e.PropertyName);
            if (e.PropertyName == nameof(Panadapter.XVTR))
            {
                _logger.LogInformation("XVTR changed");
                var xvtr = ConnectedRadio.Radio.Xvtrs.FirstOrDefault(x => x.Name == p.XVTR);
                if (xvtr == null)
                {
                    _logger.LogInformation("Could not find xvtr for {Name}", p.XVTR);
                    return;
                }
                foreach (var pan in ConnectedRadio.Radio.PanadapterList.ToList()
                             .Where(x => x != p))
                {
                    if (string.IsNullOrEmpty(pan.XVTR) || pan.XVTR == p.XVTR) continue;
                    _logger.LogInformation("Setting Panadapter XVTR from {Old} to {New}", pan.XVTR, p.XVTR);
                    _logger.LogInformation("Setting Panadapter Band from {Old} to {New}", pan.Band, p.Band);
                    //pan.Band = $"x{xvtr.Index}";
                    
                }
                
            }
        }
    }

    private async void SliceOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is Slice s)
        {
            _logger.LogInformation("Slice property {Prop} changed", e.PropertyName);
        }
    }

    protected override void RadioOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is Radio r)
        {
            if(e.PropertyName == nameof(Radio.Mox))
            {
                HandleMoxChange(r);
            }
        }
    }
    
    private void HandleMoxChange(Radio r)
    {
        if (r.FullDuplexEnabled)
        {
            var txSlice = TransmitSlice;
            if (txSlice != null)
            {
                if (r.Mox)
                {
                    _txSliceMute = txSlice.Mute;
                    if (!txSlice.Mute)
                    {
                        txSlice.Mute = true;
                    }
                }
                else
                {
                    txSlice.Mute = _txSliceMute;
                }
            }
        }
    }

}