using Flex.Smoothlake.FlexLib;
using Xunit;

namespace FlexRadioServices.Tests.Hardware;

/// <summary>
/// Verifies vendor discovery only when an operator explicitly enables hardware testing.
/// </summary>
[Collection("Hardware")]
public sealed class HardwareDiscoveryTests
{
    [HardwareFact]
    [Trait("Category", "Hardware")]
    public async Task DiscoverRadios_EnabledTrustedLan_FindsAtLeastOneRadio()
    {
        API.IsGUI = false;
        API.ProgramName = "FlexRadioServices.HardwareTests";
        API.Init();

        try
        {
            var deadline = DateTimeOffset.UtcNow + TimeSpan.FromSeconds(10);
            while (API.RadioList.Count == 0 && DateTimeOffset.UtcNow < deadline)
            {
                await Task.Delay(100);
            }

            Assert.NotEmpty(API.RadioList);
        }
        finally
        {
            API.CloseSession();
        }
    }
}
