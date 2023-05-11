using FlexRadioServices.Models;

namespace FlexRadioServices.Events;

public class VfoChangedArgs:EventArgs
{
    public VfoChangedArgs(VfoInfo vfoInfo)
    {
        VfoInfo = vfoInfo;
    }
    public VfoInfo VfoInfo { get; }
}