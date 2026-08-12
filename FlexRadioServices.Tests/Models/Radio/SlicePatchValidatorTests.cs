using FlexRadioServices.Models.Radio;
using System.Text.Json;
using Microsoft.AspNetCore.JsonPatch.SystemTextJson;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Xunit;

namespace FlexRadioServices.Tests.Models.Radio;

public sealed class SlicePatchValidatorTests
{
    [Fact]
    public void ValidateOperations_AddOperation_ReportsValidationError()
    {
        var patch = new JsonPatchDocument<SlicePatchState>();
        patch.Add(state => state.Freq, 14.074);
        var modelState = new ModelStateDictionary();

        SlicePatchValidator.ValidateOperations(patch, modelState);

        Assert.False(modelState.IsValid);
    }

    [Fact]
    public void ValidateOperations_CamelCaseAllowedReplace_DoesNotReportValidationError()
    {
        var patch = JsonSerializer.Deserialize<JsonPatchDocument<SlicePatchState>>(
            """[{"op":"replace","path":"/lock","value":true}]""")!;
        var modelState = new ModelStateDictionary();

        SlicePatchValidator.ValidateOperations(patch, modelState);

        Assert.True(modelState.IsValid);
    }

    [Fact]
    public void ValidateState_InvalidFrequencyAndLevel_ReportsValidationErrors()
    {
        var state = new SlicePatchState { Freq = double.NaN, Mode = "USB", NrLevel = 101 };
        var modelState = new ModelStateDictionary();

        SlicePatchValidator.ValidateState(state, ["USB"], modelState);

        Assert.False(modelState.IsValid);
        Assert.Contains("freq", modelState.Keys);
        Assert.Contains("nrLevel", modelState.Keys);
    }

    [Fact]
    public void ValidateState_SupportedState_DoesNotReportValidationErrors()
    {
        var state = new SlicePatchState { Freq = 14.074, Mode = "DIGU", AudioPan = 50 };
        var modelState = new ModelStateDictionary();

        SlicePatchValidator.ValidateState(state, ["DIGU"], modelState);

        Assert.True(modelState.IsValid);
    }
}
