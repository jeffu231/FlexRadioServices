using FlexRadioServices.Services;
using Xunit;

namespace FlexRadioServices.Tests.Services.Cat;

public sealed class FlexCatPortServiceTests
{
    [Theory]
    [InlineData("A1B4C604-09E0-483D-A2FB-2D18A8270268", "a1b4c604-09e0-483d-a2fb-2d18a8270268", true)]
    [InlineData("client-a", "client-b", false)]
    [InlineData("client-a", null, false)]
    public void ClientIdsMatch_UsesOrdinalIgnoreCase(string configuredClientId, string? radioClientId, bool expected)
    {
        var actual = FlexCatPortService.ClientIdsMatch(configuredClientId, radioClientId);

        Assert.Equal(expected, actual);
    }

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
