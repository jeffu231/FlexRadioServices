using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace FlexRadioServices.Models.Api;

/// <summary>
/// Validates collection-level constraints for spot submissions.
/// </summary>
public static class SpotRequestValidator
{
    /// <summary>Gets the maximum number of spots accepted in one request.</summary>
    public const int MaximumBatchSize = 100;

    /// <summary>
    /// Adds errors for an invalid spot collection.
    /// </summary>
    /// <param name="spots">The submitted spot collection.</param>
    /// <param name="modelState">The model-state dictionary that receives errors.</param>
    public static void ValidateBatch(IReadOnlyCollection<SpotRequest>? spots, ModelStateDictionary modelState)
    {
        ArgumentNullException.ThrowIfNull(modelState);

        if (spots is null || spots.Count == 0)
        {
            modelState.TryAddModelError("spots", "At least one spot is required.");
        }
        else if (spots.Count > MaximumBatchSize)
        {
            modelState.TryAddModelError("spots", $"A maximum of {MaximumBatchSize} spots is allowed per request.");
        }
    }
}
