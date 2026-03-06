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

    /// <summary>
    /// The TCP IP address of the radio
    /// </summary>
    public string Ip => _radio.IP.ToString();

    /// <summary>
    /// "dev", "Beta", "Alpha", blank for master.
    /// Populated during build of Flexlib
    /// </summary>
    public string BranchName => _radio.BranchName;
    
    /// <summary>
    /// The model name of the radio, i.e. "FLEX-6500" or "FLEX-6700"
    /// </summary>
    public string Model => _radio.Model ?? string.Empty;
    
    /// <summary>
    /// The Nickname string stored in the radio
    /// </summary>
    public string Nickname => _radio.Nickname ?? string.Empty;

    /// <summary>
    /// The Callsign string stored in the radio
    /// </summary>
    public string Callsign => _radio.Callsign ?? string.Empty;
    
    /// <summary>
    /// The serial number of the radio, including dashes
    /// </summary>
    public string Serial => _radio.Serial ?? string.Empty;

    public string Version => FlexVersion.ToString(_radio.Version);
    
    /// <summary>
    /// The status of the connection.  True when the radio
    /// is connected, false when the radio is disconnected
    /// </summary>
    public bool Connected => _radio.Connected;

    /// <summary>
    /// The state of the radio connection, i.e. "Update", "Updating", "Available", "In Use"
    /// </summary>
    public string ConnectedState => _radio.ConnectedState;

    public string Status => _radio.Status;

    public int CommandPort => _radio.CommandPort;

    public bool IsWan => _radio.IsWan;

    public string BoundClientId => _radio.BoundClientID;

    public uint ClientHandle => _radio.ClientHandle;
    
    public string GuiClientId => _radio.GUIClientID;

    public List<RadioClientProxy> GuiClients => _radio.GuiClients.Select(c => new RadioClientProxy(c)).ToList();

    public string? TransmitSlice => _radio.TransmitSlice!=null?_radio.TransmitSlice.Letter:string.Empty;

    /// <summary>
    /// The ClientHandle of the client that is transmitting. This value
    /// is set to 0 when there are no transmitting clients.
    /// </summary>
    public uint TxClientHandle => _radio.TXClientHandle;
    
}