namespace FlexRadioServices.Models;

public class VfoInfo
{
    public VfoInfo(int index, string letter, double freq, string demodMode, bool isActive, bool isTxSlice)
    {
        Index = index;
        Letter = letter;
        Freq = freq;
        DemodMode = demodMode;
        IsActive = isActive;
        IsTXSlice = isTxSlice;
    }
    public int Index { get; set; }

    public string Letter { get; set; }

    public double Freq { get; set; }
        
    public string DemodMode { get; set; }
        
    public int TuneStep { get; set; }
        
    public bool RitOn { get; set; }
        
    public bool XitOn { get; set; }
        
    public int XitFreq { get; set; }
        
    public int RitFreq { get; set; }

    public bool IsActive { get; set; }

    public bool IsTXSlice { get; set; }
}