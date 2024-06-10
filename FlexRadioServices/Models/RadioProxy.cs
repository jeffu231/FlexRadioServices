using System.ComponentModel;
using Flex.Smoothlake.FlexLib;
using Flex.UiWpfFramework.Mvvm;
using Flex.Util;
using Newtonsoft.Json;

namespace FlexRadioServices.Models;

public sealed class RadioProxy: ObservableObject
{
    private readonly Radio _radio;
    public RadioProxy(Radio radio)
    {
        _radio = radio;
        radio.PropertyChanged += RadioOnPropertyChanged;
    }

    private void RadioOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        //This is not inclusive so need to revisit this.
        RaisePropertyChanged(e.PropertyName);
    }

    [JsonIgnore]
    internal Radio Radio => _radio;

    public string Ip => _radio.IP.ToString();

    public string BranchName => _radio.BranchName;
    
    public string Model => _radio.Model ?? string.Empty;
    
    public string Nickname => _radio.Nickname ?? string.Empty;

    public string Callsign => _radio.Callsign ?? string.Empty;
    
    public string Serial => _radio.Serial ?? string.Empty;

    public string Version => FlexVersion.ToString(_radio.Version);

    public bool Connected => _radio.Connected;

    public string ConnectedState => _radio.ConnectedState;

    public string Status => _radio.Status;

    public int CommandPort => _radio.CommandPort;

    public bool IsWan => _radio.IsWan;

    public string BoundClientId => _radio.BoundClientID;

    public uint ClientHandle => _radio.ClientHandle;
    
    public string GuiClientId => _radio.GUIClientID;

    public List<RadioClientProxy> GuiClients => _radio.GuiClients.Select(c => new RadioClientProxy(c)).ToList();

    public string? TransmitSlice => _radio.TransmitSlice!=null?_radio.TransmitSlice.Letter:string.Empty;

    public uint TxClientHandle => _radio.TXClientHandle;
    
}