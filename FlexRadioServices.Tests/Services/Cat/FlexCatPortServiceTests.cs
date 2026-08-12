using FlexRadioServices.Services;
using Xunit;

namespace FlexRadioServices.Tests.Services.Cat;

public sealed class FlexCatPortServiceTests
{
    [Fact]
    public void SelectSecondaryTransmitSlice_SelectsAvailableSecondarySliceWithoutPriorTransmitState()
    {
        var selected = false;

        var response = FlexCatPortService.SelectSecondaryTransmitSlice(true, () => selected = true);

        Assert.True(selected);
        Assert.Equal(string.Empty, response);
    }

    [Fact]
    public void SetTransmitSlice_PreservesNoResponseSuccessBehavior()
    {
        var selected = false;

        Assert.Equal(string.Empty, FlexCatPortService.SetTransmitSlice(() => selected = true));
        Assert.True(selected);
    }

    [Fact]
    public void SelectSecondaryTransmitSlice_RejectsUnavailableSecondarySlice()
    {
        var selected = false;

        var response = FlexCatPortService.SelectSecondaryTransmitSlice(false, () => selected = true);

        Assert.False(selected);
        Assert.Equal("?;", response);
    }
}
