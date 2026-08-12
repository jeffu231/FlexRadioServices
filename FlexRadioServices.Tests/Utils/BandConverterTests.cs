using FlexRadioServices.Utils;
using Xunit;

namespace FlexRadioServices.Tests.Utils;

/// <summary>
/// Verifies the frequency-unit and boundary behavior of <see cref="BandConverter"/>.
/// </summary>
public sealed class BandConverterTests
{
    [Theory]
    [InlineData(135, 2190)]
    [InlineData(138, 2190)]
    [InlineData(1800, 160)]
    [InlineData(2000, 160)]
    [InlineData(14000, 20)]
    [InlineData(14350, 20)]
    [InlineData(144000, 2)]
    [InlineData(148000, 2)]
    public void ConvertToBand_ReturnsExpectedBand_ForInclusiveKilohertzBoundary(double frequency, int expectedBand)
    {
        var band = BandConverter.ConvertToBand(frequency);

        Assert.Equal(expectedBand, band);
    }

    [Theory]
    [InlineData(134.999)]
    [InlineData(138.001)]
    [InlineData(14350.001)]
    [InlineData(148000.001)]
    [InlineData(14.074)]
    [InlineData(double.NaN)]
    public void ConvertToBand_ReturnsZero_ForUnsupportedFrequency(double frequency)
    {
        var band = BandConverter.ConvertToBand(frequency);

        Assert.Equal(0, band);
    }
}
