namespace FlexRadioServices.Models.Radio;

/// <summary>
/// Describes a validated transition between two detached slice states.
/// </summary>
/// <param name="Original">The state read before applying the patch.</param>
/// <param name="Desired">The state to commit to the radio.</param>
public sealed record SliceChangeSet(SlicePatchState Original, SlicePatchState Desired)
{
    /// <summary>Gets a value that indicates whether the transition changes any supported property.</summary>
    public bool HasChanges => !Equals(Original.Freq, Desired.Freq) ||
                              !string.Equals(Original.Mode, Desired.Mode, StringComparison.Ordinal) ||
                              Original.IsTransmitSlice != Desired.IsTransmitSlice || Original.Active != Desired.Active ||
                              Original.NROn != Desired.NROn || Original.NBOn != Desired.NBOn || Original.WNBOn != Desired.WNBOn ||
                              Original.ANFOn != Desired.ANFOn || Original.APFOn != Desired.APFOn ||
                              Original.NrLevel != Desired.NrLevel || Original.NbLevel != Desired.NbLevel ||
                              Original.WnbLevel != Desired.WnbLevel || Original.AnfLevel != Desired.AnfLevel ||
                              Original.ApfLevel != Desired.ApfLevel || Original.Mute != Desired.Mute ||
                              Original.AudioGain != Desired.AudioGain || Original.AudioPan != Desired.AudioPan ||
                              Original.Lock != Desired.Lock;
}
