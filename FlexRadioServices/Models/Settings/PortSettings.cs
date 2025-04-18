namespace FlexRadioServices.Models.Settings;

public class PortSettings
{
    public string PortFriendlyName { get; set; } = "Not Named";
    
    public string Protocol { get; set; } = "TCP";

    public ushort PortNumber { get; set; }

    public PortSliceType PortSliceType { get; set; }

    public string ClientId { get; set; } = string.Empty;

    public string VfoASliceLetter { get; set; } = "A";
    
    public string VfoBSliceLetter { get; set; } = "A";
    
    public bool AutoSwitchTxSlice { get; set; }
    
}