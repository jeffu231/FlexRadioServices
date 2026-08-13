using System.ComponentModel;
using Flex.Smoothlake.FlexLib;
using FlexRadioServices.Models.Settings;
using Microsoft.Extensions.Options;

namespace FlexRadioServices.Services;

internal class RadioManagerService: ConnectedRadioServiceBase
{

    private readonly ILogger<RadioManagerService> _logger;
    private readonly IOptions<RadioSettings> _radioSettings;
    private Slice? _lastTxSlice;
    private bool _lastTxSliceMuteState;
    
    public RadioManagerService(IConnectedRadioCoordinator connectedRadioCoordinator, ILogger<RadioManagerService> logger,
        IOptions<RadioSettings> radioSettings) : base(connectedRadioCoordinator, logger)
    {
        _logger = logger;
        _radioSettings = radioSettings;
    }
    
    protected override void ConnectedRadioChanged(object? sender, ConnectedRadioEventArgs args)
    {
        if (args.PreviousRadio != null)
        {
            var previousRadio = args.PreviousRadio.Radio;
            foreach (var slice in args.PreviousRadio.Radio.SliceList)
            {
                RadioOnSliceRemoved(slice);
            }

            previousRadio.SliceAdded -= RadioOnSliceAdded;
            previousRadio.SliceRemoved -= RadioOnSliceRemoved;
            previousRadio.PanadapterAdded -= RadioOnPanadapterAdded;
            previousRadio.PanadapterRemoved -= RadioOnPanadapterRemoved;
            
            foreach (var panadapter in previousRadio.PanadapterList.ToList())
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
            ConnectedRadio.Radio.PanadapterRemoved += RadioOnPanadapterRemoved;
            
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
        //This code is mostly exploratory for now.
        if (sender is Panadapter p && (e.PropertyName == nameof(Panadapter.Band) || e.PropertyName == nameof(Panadapter.XVTR)))
        
        {
            _logger.LogDebug("Panadapter property {Prop} changed", 
                e.PropertyName);
            if (e.PropertyName == nameof(Panadapter.XVTR) && ConnectedRadio != null)
            {
                _logger.LogDebug("XVTR changed");
                var xvtr = ConnectedRadio.Radio.Xvtrs.FirstOrDefault(x => x.Name == p.XVTR);
                if (xvtr == null)
                {
                    _logger.LogDebug("Could not find xvtr for {Name}", p.XVTR);
                    return;
                }
                foreach (var pan in ConnectedRadio.Radio.PanadapterList.ToList()
                             .Where(x => x != p))
                {
                    if (string.IsNullOrEmpty(pan.XVTR) || pan.XVTR == p.XVTR) continue;
                    _logger.LogDebug("Setting Panadapter XVTR from {Old} to {New}", pan.XVTR, p.XVTR);
                    _logger.LogDebug("Setting Panadapter Band from {Old} to {New}", pan.Band, p.Band);
                    //pan.Band = $"x{xvtr.Index}";
                    
                }
                
            }
        }
    }

    private void SliceOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is not Slice slice)
        {
            return;
        }

        try
        {
            var client = slice.Radio.FindGUIClientByClientHandle(slice.ClientHandle);
            if (client is null)
            {
                _logger.LogDebug("Client {ClientHandle} is unavailable for slice {Letter} property {Property}",
                    slice.ClientHandle, slice.Letter, e.PropertyName);
                return;
            }

            var station = client.Station;
            var program = client.Program;
            _logger.LogDebug("{Station}/{Client} Slice {Letter} prop {Prop} changed",
                station, program, slice.Letter, e.PropertyName);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Skipping slice {Letter} property {Property} because its data is unavailable",
                slice.Letter, e.PropertyName);
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
                    _logger.LogDebug("TX client handle changed {Station} / {Program} / {ClientId}", 
                        client.Station, client.Program, r.TXClientHandle);
                }
                else
                {
                    _logger.LogDebug("TX client handle changed {ClientId}", r.TXClientHandle);
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
        if (r.FullDuplexEnabled && _radioSettings.Value.FullDuplexMuteLogicEnabled)
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
                        if (client is null)
                        {
                            _logger.LogInformation("TX Slice {Letter} for client handle {ClientHandle} muted",
                                txSlice.Letter, txSlice.ClientHandle);
                        }
                        else
                        {
                            var station = client.Station;
                            var program = client.Program;
                            _logger.LogInformation("TX Slice {Letter} on {Station}/{Client} muted",
                                txSlice.Letter, station, program);
                        }

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
    
    public static bool IsInterlockMox(InterlockState state)
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
