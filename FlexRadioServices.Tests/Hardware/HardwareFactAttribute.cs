using Xunit;

namespace FlexRadioServices.Tests.Hardware;

/// <summary>
/// Marks a test that may access a physical radio and skips it unless explicitly enabled.
/// </summary>
public sealed class HardwareFactAttribute : FactAttribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="HardwareFactAttribute"/> class.
    /// </summary>
    public HardwareFactAttribute()
    {
        if (!HardwareTestEnvironment.IsEnabled)
        {
            Skip = $"Set {HardwareTestEnvironment.EnableVariableName}=1 to permit hardware-in-the-loop tests on a trusted LAN.";
        }
    }
}
