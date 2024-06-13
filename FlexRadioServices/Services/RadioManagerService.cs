using System.ComponentModel;
using Flex.Smoothlake.FlexLib;

namespace FlexRadioServices.Services;

public class RadioManagerService: ConnectedRadioServiceBase
{

    private ILogger<RadioManagerService> _logger;
    private CancellationTokenSource? _cancellationToken;
    private Slice? _lastTxSlice = null;
    private bool _lastTxSliceMuteState;
    
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
            
            foreach (var panadapter in args.PreviousRadio.Radio.PanadapterList.ToList())
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
            var guiClient = s.Radio.FindGUIClientByClientHandle(s.ClientHandle);
            if (guiClient != null)
            {
                _logger.LogInformation("{Station}/{Client} Slice {Letter} prop {Prop} changed",
                    guiClient.Station, guiClient.Program, s.Letter, e.PropertyName);
            }
            else
            {
                _logger.LogInformation("Client (Null) Slice {Letter} prop {Prop} changed", s.Letter, e.PropertyName);
            }
           
        }
    }

    protected override void RadioOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is Radio r)
        {
            if(e.PropertyName == nameof(Radio.InterlockState))
            {
                _logger.LogDebug("Interlock changed {InterlockState}", r.InterlockState);
                if (r.InterlockState == InterlockState.PTTRequested ||
                    r.InterlockState == InterlockState.Ready)
                {
                    HandleMoxChange(r);
                }
                
            }
            
            if (e.PropertyName == nameof(Radio.TXClientHandle))
            {
                var client = r.FindGUIClientByClientHandle(r.TXClientHandle);
                if (client != null)
                {
                    _logger.LogInformation("TX client handle changed {Station} / {Program} / {ClientId}", 
                        client.Station, client.Program, r.TXClientHandle);
                }
                else
                {
                    _logger.LogInformation("TX client handle changed {ClientId}", r.TXClientHandle);
                }
               
            }
        }
    }
    
    /// <summary>
    /// This voodoo is to work around an issue in the Flex when Full duplex is on the transmitting slice is not muted.
    /// If you have split paths on that slice for something like a transverter, you hear own audio delayed. Full duplex 
    /// should always mute the transmit slice. So the logic checks if Full Duplex is on and the transmitting slice has
    /// a different RxAnt and TxAnt, then it mutes the slice on Tx if not muted and restores the state on Rx.
    /// </summary>
    /// <param name="r">Radio</param>
    private void HandleMoxChange(Radio r)
    {
        if (r.FullDuplexEnabled)
        {
            _logger.LogDebug("Full Duplex is on - Applying mute logic");
            if (IsInterlockMox(r.InterlockState))
            {
                var txSlice = r.SliceList.ToArray().FirstOrDefault(s => s.IsTransmitSlice && s.ClientHandle == r.TXClientHandle);
                if (txSlice != null && txSlice.RXAnt != txSlice.TXAnt)
                {
                    _lastTxSlice = txSlice;
                    _lastTxSliceMuteState = txSlice.Mute;
                    if (!txSlice.Mute)
                    {
                        var client = txSlice.Radio.FindGUIClientByClientHandle(txSlice.ClientHandle);
                        _logger.LogInformation("TX Slice {Letter} on {Station}/{Client} muted", 
                            txSlice.Letter, client.Station, client.Program);
                        txSlice.Mute = true;
                    }
                }
            }
            else
            {
                if (_lastTxSlice != null)
                {
                    _logger.LogDebug("Restoring mute state");
                    _lastTxSlice.Mute = _lastTxSliceMuteState;
                }
                _lastTxSlice = null;
                _lastTxSliceMuteState = false;
            }
        }
    }
    
    private bool IsInterlockMox(InterlockState state)
    {
        bool flag = false;
        switch (state)
        {
            case InterlockState.PTTRequested:
            case InterlockState.Transmitting:
            case InterlockState.UnkeyRequested:
                flag = true;
                break;
        }
        return flag;
    }

}