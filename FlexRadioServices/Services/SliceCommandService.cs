using Flex.Smoothlake.FlexLib;
using FlexRadioServices.Models.Radio;

namespace FlexRadioServices.Services;

/// <summary>
/// Commits a detached slice transition and attempts reverse-order restoration on a setter failure.
/// </summary>
public sealed class SliceCommandService(IFlexRadioService flexRadioService, ILogger<SliceCommandService> logger) : ISliceCommandService
{
    /// <inheritdoc/>
    public Task<SlicePatchState> ApplyAsync(SliceIdentity identity, SliceChangeSet changes, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(identity);
        ArgumentNullException.ThrowIfNull(changes);
        cancellationToken.ThrowIfCancellationRequested();

        var radio = (flexRadioService as FlexRadioService)?.GetRadioHandle(identity.RadioId)
            ?? throw new InvalidOperationException("The radio is no longer available.");
        var slice = radio.Radio.FindSliceByLetter(identity.Letter, identity.ClientHandle)
            ?? throw new InvalidOperationException("The slice no longer exists.");

        var current = SlicePatchState.FromSlice(slice);
        var effectiveChanges = new SliceChangeSet(current, changes.Desired);
        if (!effectiveChanges.HasChanges)
        {
            return Task.FromResult(current);
        }

        var applied = new Stack<Action>();
        try
        {
            Apply(slice, effectiveChanges, applied);
            return Task.FromResult(SlicePatchState.FromSlice(slice));
        }
        catch (Exception exception)
        {
            Exception? compensationException = null;
            while (applied.TryPop(out var restore))
            {
                try { restore(); }
                catch (Exception restoreException) { compensationException ??= restoreException; }
            }

            logger.LogError(exception, "Failed to update slice {Letter} on radio {RadioId}; compensation {CompensationStatus}",
                identity.Letter, identity.RadioId, compensationException is null ? "succeeded" : "failed");
            if (compensationException is not null)
            {
                logger.LogError(compensationException, "Failed to restore slice {Letter} on radio {RadioId}", identity.Letter, identity.RadioId);
            }

            throw new SliceCommandException("A radio setter failed and the radio state may have changed.", exception, compensationException);
        }
    }

    private static void Apply(Slice slice, SliceChangeSet changes, Stack<Action> applied)
    {
        var before = changes.Original;
        var after = changes.Desired;
        SetIfChanged(before.Freq, after.Freq, value => slice.Freq = value, () => slice.Freq = before.Freq, applied);
        SetIfChanged(before.Mode, after.Mode, value => slice.DemodMode = value, () => slice.DemodMode = before.Mode, applied);
        SetIfChanged(before.IsTransmitSlice, after.IsTransmitSlice, value => slice.IsTransmitSlice = value, () => slice.IsTransmitSlice = before.IsTransmitSlice, applied);
        SetIfChanged(before.Active, after.Active, value => slice.Active = value, () => slice.Active = before.Active, applied);
        SetIfChanged(before.NROn, after.NROn, value => slice.NROn = value, () => slice.NROn = before.NROn, applied);
        SetIfChanged(before.NBOn, after.NBOn, value => slice.NBOn = value, () => slice.NBOn = before.NBOn, applied);
        SetIfChanged(before.WNBOn, after.WNBOn, value => slice.WNBOn = value, () => slice.WNBOn = before.WNBOn, applied);
        SetIfChanged(before.ANFOn, after.ANFOn, value => slice.ANFOn = value, () => slice.ANFOn = before.ANFOn, applied);
        SetIfChanged(before.APFOn, after.APFOn, value => slice.APFOn = value, () => slice.APFOn = before.APFOn, applied);
        SetIfChanged(before.NrLevel, after.NrLevel, value => slice.NRLevel = value, () => slice.NRLevel = before.NrLevel, applied);
        SetIfChanged(before.NbLevel, after.NbLevel, value => slice.NBLevel = value, () => slice.NBLevel = before.NbLevel, applied);
        SetIfChanged(before.WnbLevel, after.WnbLevel, value => slice.WNBLevel = value, () => slice.WNBLevel = before.WnbLevel, applied);
        SetIfChanged(before.AnfLevel, after.AnfLevel, value => slice.ANFLevel = value, () => slice.ANFLevel = before.AnfLevel, applied);
        SetIfChanged(before.ApfLevel, after.ApfLevel, value => slice.APFLevel = value, () => slice.APFLevel = before.ApfLevel, applied);
        SetIfChanged(before.Mute, after.Mute, value => slice.Mute = value, () => slice.Mute = before.Mute, applied);
        SetIfChanged(before.AudioGain, after.AudioGain, value => slice.AudioGain = value, () => slice.AudioGain = before.AudioGain, applied);
        SetIfChanged(before.AudioPan, after.AudioPan, value => slice.AudioPan = value, () => slice.AudioPan = before.AudioPan, applied);
        SetIfChanged(before.Lock, after.Lock, value => slice.Lock = value, () => slice.Lock = before.Lock, applied);
    }

    private static void SetIfChanged<T>(T before, T after, Action<T> set, Action restore, Stack<Action> applied)
        where T : IEquatable<T>
    {
        if (before.Equals(after))
        {
            return;
        }

        set(after);
        applied.Push(restore);
    }
}
