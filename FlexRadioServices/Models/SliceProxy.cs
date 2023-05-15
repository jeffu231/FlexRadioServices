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

    public bool IsTransmitSlice => _flexSlice.IsTransmitSlice;
    
    public bool Active => _flexSlice.Active;

    public int Index => _flexSlice.Index;

    public string Letter => _flexSlice.Letter;

    public double Freq => _flexSlice.Freq;

    public int Step => _flexSlice.TuneStep;
    
    public string Mode => _flexSlice.DemodMode ?? string.Empty;

    public bool NrOn => _flexSlice.NROn;

    public bool NbOn => _flexSlice.NBOn;

    public bool WNBOn => _flexSlice.WNBOn;

    public bool ANFOn => _flexSlice.ANFOn;

    public bool APFOn => _flexSlice.APFOn;

    public int NrLevel => _flexSlice.NRLevel;
    
    public int NbLevel => _flexSlice.NBLevel;

    public int WNBLevel => _flexSlice.WNBLevel;

    public int ANFLevel => _flexSlice.ANFLevel;

    public int APFLevel => _flexSlice.APFLevel;

    public bool Mute => _flexSlice.Mute;

    public int AudioGain => _flexSlice.AudioGain;

    public int AudioPan => _flexSlice.AudioPan;

    [JsonConverter(typeof(StringEnumConverter))]
    public AGCMode AGCMode => _flexSlice.AGCMode;

    public int AGCOffLevel => _flexSlice.AGCOffLevel;
    // {
    //   get
    //   {
    //     if (this._flexSlice == null)
    //       return string.Empty;
    //     return this._flexSlice.AGCMode == AGCMode.Off ? this._flexSlice.AGCOffLevel.ToString() : this._flexSlice.AGCThreshold.ToString();
    //   }
    // }

    public int AGCThreshold => _flexSlice.AGCThreshold;

    public bool DiversityOn => _flexSlice.DiversityOn;

    public bool Lock => _flexSlice.Lock;

    public bool RecordOn => _flexSlice.RecordOn;

    public bool PlayOn => _flexSlice.PlayOn;

    public bool XITOn => _flexSlice.XITOn;

    public bool RITOn => _flexSlice.RITOn;

    public int XITFreq => _flexSlice.XITFreq;

    public int RITFREQ => _flexSlice.RITFreq;
    
    public string FilterWidth => Math.Abs(_flexSlice.FilterHigh - _flexSlice.FilterLow).ToString();

    public int FilterHigh => _flexSlice.FilterHigh;

    public int FilterLow => _flexSlice.FilterLow;

    public int RFGain => _flexSlice.Panadapter.RFGain;

    public string RXAnt => _flexSlice.RXAnt;

    public string TXAnt => _flexSlice.TXAnt;

    public double Pan => _flexSlice.Panadapter.Bandwidth * 1000.0;

    public string Band => _flexSlice.Panadapter.Band;

    public string XVTR => _flexSlice.Panadapter.XVTR;
    // {
    //   get
    //   {
    //     if (this._flexSlice == null)
    //       return string.Empty;
    //     if (this._flexSlice.Panadapter.Band == \u0023\u003DzMojhtdNimdXEGJzxoUsBJobsYbW0.\u0023\u003DzxHQnTr0\u003D(-1467668293))
    //       return \u0023\u003DzMojhtdNimdXEGJzxoUsBJobsYbW0.\u0023\u003DzxHQnTr0\u003D(-1467668268);
    //     return !(this._flexSlice.Panadapter.Band == \u0023\u003DzMojhtdNimdXEGJzxoUsBJobsYbW0.\u0023\u003DzxHQnTr0\u003D(-1467668318)) ? this._flexSlice.Panadapter.Band : \u0023\u003DzMojhtdNimdXEGJzxoUsBJobsYbW0.\u0023\u003DzxHQnTr0\u003D(-1467668258);
    //   }
    // }

    public int DAXChannel => this._flexSlice.DAXChannel;
}