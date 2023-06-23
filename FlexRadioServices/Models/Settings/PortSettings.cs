namespace FlexRadioServices.Models.Ports;

public class PortSettings
{
    public string Protocol { get; set; } = "TCP";

    public ushort PortNumber { get; set; }

    public PortSliceType PortSliceType { get; set; }

    public string VfoASliceLetter { get; set; } = "A";
    
    public string VfoBSliceLetter { get; set; } = "A";
    
    public bool AutoSwitchTxSlice { get; set; }
    
}