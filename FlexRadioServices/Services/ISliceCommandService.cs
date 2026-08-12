using FlexRadioServices.Models.Radio;

namespace FlexRadioServices.Services;

/// <summary>
/// Commits validated slice changes to a radio.
/// </summary>
public interface ISliceCommandService
{
    /// <summary>Applies a set of validated changes to a currently existing slice.</summary>
    /// <param name="identity">The identity of the slice to update.</param>
    /// <param name="changes">The validated transition to apply.</param>
    /// <param name="cancellationToken">A token used to cancel the operation before it begins.</param>
    /// <returns>The committed detached state.</returns>
    /// <exception cref="InvalidOperationException">The identified slice no longer exists.</exception>
    /// <exception cref="SliceCommandException">A radio setter failed; compensation was attempted.</exception>
    Task<SlicePatchState> ApplyAsync(SliceIdentity identity, SliceChangeSet changes, CancellationToken cancellationToken);
}
