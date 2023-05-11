using System.Collections.Concurrent;
using System.ComponentModel;
using Flex.Smoothlake.FlexLib;
using FlexRadioServices.Events;

namespace FlexRadioServices.Models;

public class ActiveState
{
    private uint _boundClientHandle = 0;
    private readonly ILogger<ActiveState> _logger;

    public ActiveState(ILogger<ActiveState> logger)
    {
        _logger = logger;
        Clients = new ConcurrentBag<RadioClient>();
        Radios = new ConcurrentDictionary<string, FlexRadio>();
    }
    public Radio? ActiveRadio { get; set; }

    public ConcurrentDictionary<string, FlexRadio> Radios { get; private set; }

    public ConcurrentBag<RadioClient> Clients { get; private set; }

    public void ConnectToRadio(Radio radio)
    {
        if (ActiveRadio != null && ActiveRadio == radio)
        {
            return;
        }
        
        if (ActiveRadio != null)
        {
            DisconnectRadio(ActiveRadio);
        }
        
        radio.PropertyChanged += ActiveRadioOnPropertyChanged;
        radio.SliceAdded += ActiveRadioOnSliceAdded;
        radio.SliceRemoved += ActiveRadioOnSliceRemoved;
        radio.GUIClientAdded += ActiveRadioOnGUIClientAdded;
        radio.GUIClientRemoved += ActiveRadioOnGUIClientRemoved;
        
        if (radio.Connected) return;
        
        radio.Connect();

        if (!radio.Connected)
        {
            _logger.LogError("Couldn't connect to Radio");
            return;
        }

        ActiveRadio = radio;
        _logger.LogDebug("Connected!");
        
        Clients.Clear();
        
        var clients = radio.GuiClients.Select(gc =>
            new RadioClient(gc.ClientID, gc.ClientHandle, gc.Station, gc.Program));
        foreach (var client in clients)
        {
            Clients.Add(client);
        }
    }

    public void DisconnectRadio(Radio radio)
    {
        radio.PropertyChanged -= ActiveRadioOnPropertyChanged;
        radio.SliceAdded -= ActiveRadioOnSliceAdded;
        radio.SliceRemoved -= ActiveRadioOnSliceRemoved;
        radio.GUIClientAdded -= ActiveRadioOnGUIClientAdded;
        radio.GUIClientRemoved -= ActiveRadioOnGUIClientRemoved;
        
        radio.Disconnect();
        
        _logger.LogDebug("Disconnected");
        
        Clients.Clear();
    }

    private void ActiveRadioOnGUIClientRemoved(GUIClient guiClient)
    {
        
        
    }

    private void ActiveRadioOnGUIClientAdded(GUIClient guiClient)
    {
        
        
    }


    private void ActiveRadioOnSliceRemoved(Slice slc)
    {
        RemoveSliceListeners(slc);
    }

    private void ActiveRadioOnSliceAdded(Slice slc)
    {
        AddSliceListeners(slc);
    }

    private void ActiveRadioOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is @"PersistenceLoaded" && ActiveRadio != null)
        {
            ActiveRadio.PropertyChanged -= ActiveRadioOnPropertyChanged;
            //Ready = true;
            ConfigureListeners();
        }
    }

    private void ConfigureListeners()
    {
        if (ActiveRadio != null && ActiveRadio.Connected)
        {
            lock (ActiveRadio.SliceList)
            {
                foreach (Slice slc in ActiveRadio.SliceList)
                {
                    AddSliceListeners(slc);
                }
            }
        }
    }

    private void AddSliceListeners(Slice slc)
    {
        if (ActiveRadio?.BoundClientID != null)
        {
            if (slc.ClientHandle == _boundClientHandle)
            {
                slc.PropertyChanged += SliceOnPropertyChanged;
            }
        }
        else
        {
            slc.PropertyChanged += SliceOnPropertyChanged;
        }
    }

    private void RemoveSliceListeners(Slice slc)
    {
        slc.PropertyChanged -= SliceOnPropertyChanged;
    }

    private void SliceOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is Slice slice)
        {
            switch (e.PropertyName)
            {
                case "Active":
                case "Freq":
                case "TuneStep":
                case "RITOn":
                case "XITOn":
                case "RITFreq":
                case "XITFreq":
                case "DemodMode":
                case "IsTransmitSlice":
                    var vfoInfo = CreateSliceInfo(slice);
                    if (vfoInfo != null)
                    {
                        OnVfoChanged(vfoInfo);
                    }

                    break;
            }
        }
    }

    /// <summary>
    /// Returns the Active slice for the bound radio, or the first active slice found if a client is not bound.
    /// </summary>
    /// <returns>SliceInfo</returns>
    public VfoInfo? ActiveSliceInfo()
    {
        return CreateSliceInfo(ActiveRadio?.ActiveSlice);
    }

    /// <summary>
    /// Returns the Transmit slice for the bound client or the first Transmit slice if a client is not bound.
    /// </summary>
    /// <returns>SliceInfo</returns>
    public VfoInfo? TransmitSliceInfo()
    {
        if (ActiveRadio != null)
            lock (ActiveRadio.SliceList)
            {
                foreach (Slice slc in ActiveRadio.SliceList)
                {
                    if (ActiveRadio.BoundClientID != null)
                    {
                        if (slc.IsTransmitSlice && slc.ClientHandle == _boundClientHandle)
                        {
                            return CreateSliceInfo(slc);
                        }
                    }
                    else
                    {
                        if (slc.IsTransmitSlice)
                        {
                            return CreateSliceInfo(slc);
                        }
                    }
                }
            }

        return null;
    }

    private static VfoInfo? CreateSliceInfo(Slice? s)
    {
        if (s != null)
        {
            VfoInfo? si = null;
            si = new VfoInfo(s.Index, s.Letter, s.Freq, s.DemodMode, s.Active, s.IsTransmitSlice);
            si.TuneStep = s.TuneStep;
            si.RitOn = s.RITOn;
            si.XitOn = s.XITOn;
            si.RitFreq = s.RITFreq;
            si.XitFreq = s.XITFreq;
            return si;
        }

        return null;
    }

    public event EventHandler<VfoChangedArgs> VfoChanged = null!;

    private void OnVfoChanged(VfoInfo vfoInfo)
    {
        VfoChanged?.Invoke(this, new VfoChangedArgs(vfoInfo));
    }
}