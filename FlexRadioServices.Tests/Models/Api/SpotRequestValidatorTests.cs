using System.ComponentModel.DataAnnotations;
using FlexRadioServices.Models.Api;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Xunit;

namespace FlexRadioServices.Tests.Models.Api;

/// <summary>
/// Verifies the request and collection validation applied to v2 spot submissions.
/// </summary>
public sealed class SpotRequestValidatorTests
{
    [Fact]
    public void ValidateBatch_OverMaximum_ReportsValidationError()
    {
        var modelState = new ModelStateDictionary();
        var spots = Enumerable.Range(0, SpotRequestValidator.MaximumBatchSize + 1)
            .Select(_ => CreateValidSpot())
            .ToList();

        SpotRequestValidator.ValidateBatch(spots, modelState);

        Assert.False(modelState.IsValid);
        Assert.Contains("spots", modelState.Keys);
    }

    [Fact]
    public void Validate_InvalidFields_ReportsValidationErrors()
    {
        var spot = CreateValidSpot() with
        {
            Callsign = " ",
            RxFrequency = double.NaN,
            Color = "blue",
            TriggerAction = "transmit"
        };
        var validationResults = new List<ValidationResult>();

        var valid = Validator.TryValidateObject(spot, new ValidationContext(spot), validationResults, true);

        Assert.False(valid);
        Assert.NotEmpty(validationResults);
    }

    private static SpotRequest CreateValidSpot() => new()
    {
        Callsign = "K1ABC",
        RxFrequency = 14.074,
        TxFrequency = 14.074,
        Mode = "DIGU",
        Timestamp = DateTime.UtcNow
    };
}
