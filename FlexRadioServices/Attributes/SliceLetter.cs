using System.ComponentModel.DataAnnotations;

namespace FlexRadioServices.Attributes;

public class SliceLetter:RegularExpressionAttribute
{
    public SliceLetter():base(@"^[A-E]$")
    {
        ErrorMessage = "Slice letter must be A - E";
    }
    
    
}