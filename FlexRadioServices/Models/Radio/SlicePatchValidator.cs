using Flex.Smoothlake.FlexLib;
using Microsoft.AspNetCore.JsonPatch.SystemTextJson;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace FlexRadioServices.Models.Radio;

/// <summary>
/// Validates the supported JSON Patch operations and resulting detached slice state.
/// </summary>
public static class SlicePatchValidator
{
    private static readonly HashSet<string> AllowedPaths = new(StringComparer.Ordinal)
    {
        "/freq", "/mode", "/isTransmitSlice", "/active", "/nrOn", "/nbOn", "/wnbOn", "/anfOn", "/apfOn",
        "/nrLevel", "/nbLevel", "/wnbLevel", "/anfLevel", "/apfLevel", "/mute", "/audioGain", "/audioPan", "/lock"
    };

    /// <summary>Validates that each patch operation is an explicitly supported replacement.</summary>
    /// <param name="patch">The patch document to validate.</param>
    /// <param name="modelState">The model-state dictionary to receive validation errors.</param>
    public static void ValidateOperations(JsonPatchDocument<SlicePatchState> patch, ModelStateDictionary modelState)
    {
        ArgumentNullException.ThrowIfNull(patch);
        ArgumentNullException.ThrowIfNull(modelState);

        foreach (var operation in patch.Operations)
        {
            if (!string.Equals(operation.op, "replace", StringComparison.Ordinal))
            {
                modelState.TryAddModelError(operation.path ?? string.Empty, "Only the 'replace' JSON Patch operation is supported.");
            }
            else if (operation.path is null || !AllowedPaths.Contains(operation.path))
            {
                modelState.TryAddModelError(operation.path ?? string.Empty, $"The path '{operation.path}' is not supported.");
            }
        }
    }

    /// <summary>Validates semantic constraints after a patch has been applied to detached state.</summary>
    /// <param name="state">The detached state to validate.</param>
    /// <param name="availableModes">The modes currently supported by the live slice.</param>
    /// <param name="modelState">The model-state dictionary to receive validation errors.</param>
    public static void ValidateState(SlicePatchState state, IReadOnlyCollection<string> availableModes, ModelStateDictionary modelState)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(availableModes);
        ArgumentNullException.ThrowIfNull(modelState);

        if (!double.IsFinite(state.Freq) || state.Freq is < 0.001 or > 10000.0)
        {
            modelState.TryAddModelError("freq", "Frequency must be a finite value between 0.001 and 10000 MHz.");
        }

        if (string.IsNullOrWhiteSpace(state.Mode) || !availableModes.Contains(state.Mode, StringComparer.OrdinalIgnoreCase))
        {
            modelState.TryAddModelError("mode", "Mode must be supported by the slice.");
        }

        ValidateRange(state.NrLevel, "nrLevel", modelState);
        ValidateRange(state.NbLevel, "nbLevel", modelState);
        ValidateRange(state.WnbLevel, "wnbLevel", modelState);
        ValidateRange(state.AnfLevel, "anfLevel", modelState);
        ValidateRange(state.ApfLevel, "apfLevel", modelState);
        ValidateRange(state.AudioGain, "audioGain", modelState);
        ValidateRange(state.AudioPan, "audioPan", modelState);
    }

    private static void ValidateRange(int value, string field, ModelStateDictionary modelState)
    {
        if (value is < 0 or > 100)
        {
            modelState.TryAddModelError(field, $"{field} must be between 0 and 100.");
        }
    }
}
