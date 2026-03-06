using System.ComponentModel.DataAnnotations;
using Flex.Smoothlake.FlexLib;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace FlexRadioServices.Models;

public class SliceProxy
{
    
    private readonly Slice _flexSlice;

    public SliceProxy(Slice slc) => _flexSlice = slc;

    [JsonIgnore]
    internal Slice Slice => _flexSlice;

    /// <summary>
    /// Client handle of the GUI client that owns the Slice.
    /// </summary>
    public uint ClientHandle => _flexSlice.ClientHandle;

    public string Owner => _flexSlice.Owner;

    /// <summary>
    /// Gets or sets whether the Slice is the transmit slice on the GUI client.
    /// </summary>
    public bool IsTransmitSlice
    {
        get => _flexSlice.IsTransmitSlice;
        set => _flexSlice.IsTransmitSlice = value;
    }
    
    /// <summary>
    /// Gets or sets whether the Slice is the Active Slice.
    /// </summary>
    public bool Active
    {
        get => _flexSlice.Active;
        set => _flexSlice.Active = value;
    }

    /// <summary>
    /// Gets the slice index of the Slice.
    /// </summary>
    public int Index => _flexSlice.Index;

    [RegularExpression(@"^[A-E]$", ErrorMessage = "Slice letter must be A - E")]
    public string Letter => _flexSlice.Letter;

    public double Freq
    {
        get => _flexSlice.Freq;
        set => _flexSlice.Freq = value;
    }

    public int Step
    {
        get => _flexSlice.TuneStep;
        set => _flexSlice.TuneStep = value;
    }

    /// <summary>
    /// Gets or sets the demodulation mode for the slice as a string: 
    /// "USB", "DIGU", "LSB", "DIGL", "CW", "DSB", "AM", "SAM", "FM"
    /// </summary>
    public string Mode
    {
        get => _flexSlice.DemodMode ?? string.Empty;
        set => _flexSlice.DemodMode = value;
    }
    
    public bool NROn
    {
        get => _flexSlice.NROn;
        set => _flexSlice.NROn = value;
    }

    public bool NBOn
    {
        get => _flexSlice.NBOn;
        set => _flexSlice.NBOn = value;
    }

    public bool WNBOn
    {
        get => _flexSlice.WNBOn;
        set => _flexSlice.WNBOn = value;
    }

    public bool ANFOn
    {
        get => _flexSlice.ANFOn;
        set => _flexSlice.ANFOn = value;
    }

    public bool APFOn
    {
        get => _flexSlice.APFOn;
        set => _flexSlice.APFOn = value;
    }

    public int NrLevel
    {
        get => _flexSlice.NRLevel;
        set => _flexSlice.NRLevel = value;
    }

    public int NbLevel
    {
        get => _flexSlice.NBLevel;
        set => _flexSlice.NBLevel = value;
    }

    public int WNBLevel
    {
        get => _flexSlice.WNBLevel;
        set => _flexSlice.WNBLevel = value;
    }

    public int ANFLevel
    {
        get => _flexSlice.ANFLevel;
        set => _flexSlice.ANFLevel = value;
    }

    public int APFLevel
    {
        get => _flexSlice.APFLevel;
        set => _flexSlice.APFLevel = value;
    }

    public bool Mute
    {
        get => _flexSlice.Mute;
        set => _flexSlice.Mute = value;
    }

    public int AudioGain
    {
        get => _flexSlice.AudioGain;
        set => _flexSlice.AudioGain = value;
    }

    /// <summary>
    /// Gets or sets the left-right pan for the Slice audio from 0 to 100.  
    /// A value of 50 pans evenly between left and right.
    /// </summary>
    public int AudioPan
    {
        get => _flexSlice.AudioPan;
        set => _flexSlice.AudioPan = value;
    }

    /// <summary>
    /// Gets or sets the current AGC mode for the Slice.
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public AGCMode AGCMode
    {
        get => _flexSlice.AGCMode;
        set => _flexSlice.AGCMode = value;
    }

    public int AGCOffLevel
    {
        get => _flexSlice.AGCOffLevel;
        set => _flexSlice.AGCOffLevel = value;
    }
    
    public int AGCThreshold
    {
        get => _flexSlice.AGCThreshold;
        set => _flexSlice.AGCThreshold = value;
    }

    public bool DiversityOn
    {
        get => _flexSlice.DiversityOn;
        set => _flexSlice.DiversityOn = value;
    }

    /// <summary>
    /// Gets or sets whether the Slice is locked.  When locked, 
    /// the Slice frequency cannot be changed.
    /// </summary>
    public bool Lock
    {
        get => _flexSlice.Lock;
        set => _flexSlice.Lock = value;
    }

    public bool RecordOn
    {
        get => _flexSlice.RecordOn;
        set => _flexSlice.RecordOn = value;
    }

    public bool PlayOn
    {
        get => _flexSlice.PlayOn;
        set => _flexSlice.PlayOn = value;
    }

    public bool XITOn
    {
        get => _flexSlice.XITOn;
        set => _flexSlice.XITOn = value;
    }

    public bool RITOn
    {
        get => _flexSlice.RITOn;
        set => _flexSlice.RITOn = value;
    }

    public int XITFreq
    {
        get => _flexSlice.XITFreq;
        set => _flexSlice.XITFreq = value;
    }

    public int RITFREQ
    {
        get => _flexSlice.RITFreq;
        set => _flexSlice.RITFreq = value;
    }

    public string FilterWidth => Math.Abs(_flexSlice.FilterHigh - _flexSlice.FilterLow).ToString();

    public int FilterHigh
    {
        get => _flexSlice.FilterHigh;
        set => _flexSlice.FilterHigh = value;
    }

    public int FilterLow
    {
        get => _flexSlice.FilterLow;
        set => _flexSlice.FilterLow = value;
    }

    /// <summary>
    /// Gets Sets the RFGain on the Panadapter this Slice is attached to. 
    /// </summary>
    public int RFGain
    {
        get => _flexSlice.Panadapter.RFGain;
        set => _flexSlice.Panadapter.RFGain = value;
    }

    public string RXAnt
    {
        get => _flexSlice.RXAnt;
        set => _flexSlice.RXAnt = value;
    }

    /// <summary>
    /// Gets or sets the transmit antenna for the slice as a string:
    /// "ANT1", "ANT2", "XVTR"
    /// </summary>
    public string TXAnt
    {
        get => _flexSlice.TXAnt;
        set => _flexSlice.TXAnt = value;
    }

    /// <summary>
    ///  Gets or sets the bandwidth for the Panadapter this slice is attached to.
    /// </summary>
    public double Pan
    {
        get => _flexSlice.Panadapter.Bandwidth * 1000.0;
        set => _flexSlice.Panadapter.Bandwidth = value / 1000;
    }

    public string Band
    {
        get => _flexSlice.Panadapter.Band;
        set => _flexSlice.Panadapter.Band = value;
    }

    /// <summary>
    /// Xvtr setting for the Panadapter this slice is attached to.
    /// </summary>
    public string XVTR
    {
        get => _flexSlice.Panadapter.XVTR;
        set => _flexSlice.Panadapter.XVTR = value;
    }
    
    /// <summary>
    /// Gets or sets the DAX Channel for the Slice, from 0 to 8
    /// </summary>
    public int DAXChannel
    {
        get => this._flexSlice.DAXChannel;
        set => this._flexSlice.DAXChannel = value;
    }
}