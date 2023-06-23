using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using Flex.Smoothlake.FlexLib;
using FlexRadioServices.Events;
using FlexRadioServices.Services;

namespace FlexRadioServices.Models;

public class RadioState
{
    //private uint _boundClientHandle = 0;
    //private readonly ILogger<RadioState> _logger;
    //private readonly MqttClientService _mqttClient;
    //private readonly ObservableCollection<RadioProxy> _radios;

    // public RadioState(ILogger<RadioState> logger, MqttClientService mqttClientService)
    // {
    //     _logger = logger;
    //     Clients = new ConcurrentBag<RadioClient>();
    //     _radios = new ObservableCollection<RadioProxy>();
    //     _mqttClient = mqttClientService;
    //     
    // }
    //
    // public ObservableCollection<RadioProxy> Radios { get; private set; }
    //
    // public ConcurrentBag<RadioClient> Clients { get; private set; }
    //
    // public void ConnectToRadio(Radio radio)
    // {
    //     radio.PropertyChanged += RadioOnPropertyChanged;
    //     radio.SliceAdded += RadioOnSliceAdded;
    //     radio.SliceRemoved += RadioOnSliceRemoved;
    //     radio.GUIClientAdded += RadioOnGUIClientAdded;
    //     radio.GUIClientRemoved += RadioOnGUIClientRemoved;
    //     
    //     if (radio.Connected) return;
    //     
    //     radio.Connect();
    //
    //     if (!radio.Connected)
    //     {
    //         _logger.LogError("Couldn't connect to Radio");
    //         return;
    //     }
    //     
    //     _logger.LogDebug("Connected!");
    //     
    //     Clients.Clear();
    //     
    //     var clients = radio.GuiClients.Select(gc =>
    //         new RadioClient(gc.ClientID, gc.ClientHandle, gc.Station, gc.Program));
    //     foreach (var client in clients)
    //     {
    //         Clients.Add(client);
    //     }
    //     
    //     //AddSliceListeners(radio);
    // }
    //
    // public void DisconnectRadio(Radio radio)
    // {
    //     RemoveSliceListeners(radio);
    //     radio.PropertyChanged -= RadioOnPropertyChanged;
    //     radio.SliceAdded -= RadioOnSliceAdded;
    //     radio.SliceRemoved -= RadioOnSliceRemoved;
    //     radio.GUIClientAdded -= RadioOnGUIClientAdded;
    //     radio.GUIClientRemoved -= RadioOnGUIClientRemoved;
    //     
    //     radio.Disconnect();
    //     
    //     _logger.LogDebug("Disconnected");
    //     
    //     Clients.Clear();
    // }
    //
    // private void RadioOnGUIClientRemoved(GUIClient guiClient)
    // {
    //     
    //     
    // }
    //
    // private void RadioOnGUIClientAdded(GUIClient guiClient)
    // {
    //     
    //     
    // }
    //
    //
    // private void RadioOnSliceRemoved(Slice slc)
    // {
    //     _logger.LogDebug("Slice Removed");
    //     RemoveSliceListener(slc);
    // }
    //
    // private void RadioOnSliceAdded(Slice slc)
    // {
    //     _logger.LogDebug("Slice Added");
    //     AddSliceListener(slc);
    // }
    //
    // private void RadioOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    // {
    //     //sender is a Radio
    //     //_logger.LogDebug($"Radio property changed sender is Radio {sender}", sender is Radio);
    //     // if (e.PropertyName is @"PersistenceLoaded" && sender is Radio radio)
    //     // {
    //     //     radio.PropertyChanged -= RadioOnPropertyChanged;
    //     //     //Ready = true;
    //     //     ConfigureListeners(radio);
    //     // }
    // }
    //
    // private void AddSliceListeners(Radio radio)
    // {
    //     lock (radio.SliceList)
    //     {
    //         foreach (Slice slc in radio.SliceList)
    //         {
    //             AddSliceListener(slc);
    //         }
    //     }
    // }
    //
    // private void RemoveSliceListeners(Radio radio)
    // {
    //     lock (radio.SliceList)
    //     {
    //         foreach (Slice slc in radio.SliceList)
    //         {
    //             RemoveSliceListener(slc);
    //         }
    //     }
    // }
    //
    // private void AddSliceListener(Slice slc)
    // {
    //     _logger.LogDebug("Added slice {Letter} listener for radio {RadioSerial}",slc.Letter, slc.Radio.Serial);
    //     slc.PropertyChanged += SliceOnPropertyChanged;
    // }
    //
    // private void RemoveSliceListener(Slice slc)
    // {
    //     _logger.LogDebug("Removed slice {Letter} listener for radio {RadioSerial}",slc.Letter, slc.Radio.Serial);
    //     slc.PropertyChanged -= SliceOnPropertyChanged;
    // }
    //
    // private async void SliceOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    // {
    //     if (sender is Slice slice)
    //     {
    //         var prop = System.Text.Json.JsonNamingPolicy.CamelCase.ConvertName(e.PropertyName);
    //         await _mqttClient.Publish($"radios/{slice.Radio.Serial}/slice/{slice.Letter}/{prop}", slice.Freq.ToString(CultureInfo.InvariantCulture));
    //         //_logger.LogDebug("Slice {Letter} property {PropertyName} for radio {RadioSerial} changed", slice.Letter, e.PropertyName, slice.Radio.Serial);
    //         // switch (e.PropertyName)
    //         // {
    //         //     case "Active":
    //         //     case "Freq":
    //         //         await _mqttClient.Publish($"radios/{slice.Radio.Serial}/slice/{slice.Letter}/freq", slice.Freq.ToString(CultureInfo.InvariantCulture));
    //         //         break;
    //         //     case "TuneStep":
    //         //     case "RITOn":
    //         //     case "XITOn":
    //         //     case "RITFreq":
    //         //     case "XITFreq":
    //         //     case "DemodMode":
    //         //     case "IsTransmitSlice":
    //         //         var vfoInfo = CreateSliceInfo(slice);
    //         //         if (vfoInfo != null)
    //         //         {
    //         //             OnVfoChanged(vfoInfo);
    //         //         }
    //         //
    //         //         break;
    //         // }
    //     }
    // }
    //
    // /// <summary>
    // /// Returns the Active slice for the bound radio, or the first active slice found if a client is not bound.
    // /// </summary>
    // /// <returns>SliceInfo</returns>
    // public VfoInfo? ActiveSliceInfo(string radioSerial)
    // {
    //     if (Radios.TryGetValue(radioSerial, out var radio))
    //     {
    //         return CreateSliceInfo(radio.Radio.ActiveSlice);
    //     }
    //     
    //     return null;
    // }
    //
    // /// <summary>
    // /// Returns the Transmit slice for the bound client or the first Transmit slice if a client is not bound.
    // /// </summary>
    // /// <returns>SliceInfo</returns>
    // public VfoInfo? TransmitSliceInfo(string radioSerial)
    // {
    //     
    //     // if (Radios.TryGetValue(radioSerial, out var radio))
    //     // {
    //     //     lock (radio.Radio.SliceList)
    //     //     {
    //     //         foreach (Slice slc in radio.Radio.SliceList)
    //     //         {
    //     //             if (ActiveRadio.BoundClientID != null)
    //     //             {
    //     //                 if (slc.IsTransmitSlice && slc.ClientHandle == _boundClientHandle)
    //     //                 {
    //     //                     return CreateSliceInfo(slc);
    //     //                 }
    //     //             }
    //     //             else
    //     //             {
    //     //                 if (slc.IsTransmitSlice)
    //     //                 {
    //     //                     return CreateSliceInfo(slc);
    //     //                 }
    //     //             }
    //     //         }
    //     //     }
    //     // }
    //     // if (ActiveRadio != null)
    //     //     lock (ActiveRadio.SliceList)
    //     //     {
    //     //         foreach (Slice slc in ActiveRadio.SliceList)
    //     //         {
    //     //             if (ActiveRadio.BoundClientID != null)
    //     //             {
    //     //                 if (slc.IsTransmitSlice && slc.ClientHandle == _boundClientHandle)
    //     //                 {
    //     //                     return CreateSliceInfo(slc);
    //     //                 }
    //     //             }
    //     //             else
    //     //             {
    //     //                 if (slc.IsTransmitSlice)
    //     //                 {
    //     //                     return CreateSliceInfo(slc);
    //     //                 }
    //     //             }
    //     //         }
    //     //     }
    //
    //     return null;
    // }
    //
    // private static VfoInfo? CreateSliceInfo(Slice? s)
    // {
    //     if (s != null)
    //     {
    //         VfoInfo? si = null;
    //         si = new VfoInfo(s.Index, s.Letter, s.Freq, s.DemodMode, s.Active, s.IsTransmitSlice);
    //         si.TuneStep = s.TuneStep;
    //         si.RitOn = s.RITOn;
    //         si.XitOn = s.XITOn;
    //         si.RitFreq = s.RITFreq;
    //         si.XitFreq = s.XITFreq;
    //         return si;
    //     }
    //
    //     return null;
    // }
    //
    // public event EventHandler<VfoChangedArgs> VfoChanged = null!;
    //
    // private void OnVfoChanged(VfoInfo vfoInfo)
    // {
    //     VfoChanged?.Invoke(this, new VfoChangedArgs(vfoInfo));
    // }
}