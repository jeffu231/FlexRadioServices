using System.Text.RegularExpressions;

namespace FlexRadioServices.Utils;

public static class Validation
{
    private const string SlicePattern = $"^[A-E]$";

    public static bool IsValidSliceLetter(string letter)
    {
        var m = Regex.Match(letter, SlicePattern);
        return m.Success;
    }
}