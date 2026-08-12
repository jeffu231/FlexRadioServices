using Flex.Smoothlake.FlexLib;

namespace FlexRadioServices.Models.Radio;

/// <summary>
/// Represents the supported, detached state of a slice that can be changed by JSON Patch.
/// </summary>
public sealed class SlicePatchState
{
    /// <summary>Gets or sets the slice frequency in MHz.</summary>
    public double Freq { get; set; }

    /// <summary>Gets or sets the demodulation mode.</summary>
    public string Mode { get; set; } = string.Empty;

    /// <summary>Gets or sets a value that indicates whether the slice is the transmit slice.</summary>
    public bool IsTransmitSlice { get; set; }

    /// <summary>Gets or sets a value that indicates whether the slice is active.</summary>
    public bool Active { get; set; }

    /// <summary>Gets or sets a value that indicates whether noise reduction is enabled.</summary>
    public bool NROn { get; set; }

    /// <summary>Gets or sets a value that indicates whether noise blanking is enabled.</summary>
    public bool NBOn { get; set; }

    /// <summary>Gets or sets a value that indicates whether wide noise blanking is enabled.</summary>
    public bool WNBOn { get; set; }

    /// <summary>Gets or sets a value that indicates whether automatic notch filtering is enabled.</summary>
    public bool ANFOn { get; set; }

    /// <summary>Gets or sets a value that indicates whether audio peak filtering is enabled.</summary>
    public bool APFOn { get; set; }

    /// <summary>Gets or sets the noise-reduction level.</summary>
    public int NrLevel { get; set; }

    /// <summary>Gets or sets the noise-blanker level.</summary>
    public int NbLevel { get; set; }

    /// <summary>Gets or sets the wide-noise-blanker level.</summary>
    public int WnbLevel { get; set; }

    /// <summary>Gets or sets the automatic-notch-filter level.</summary>
    public int AnfLevel { get; set; }

    /// <summary>Gets or sets the audio-peak-filter level.</summary>
    public int ApfLevel { get; set; }

    /// <summary>Gets or sets a value that indicates whether slice audio is muted.</summary>
    public bool Mute { get; set; }

    /// <summary>Gets or sets the audio gain.</summary>
    public int AudioGain { get; set; }

    /// <summary>Gets or sets the left-to-right audio pan position.</summary>
    public int AudioPan { get; set; }

    /// <summary>Gets or sets a value that indicates whether the slice frequency is locked.</summary>
    public bool Lock { get; set; }

    /// <summary>Creates a detached state copied from a live FlexLib slice.</summary>
    /// <param name="slice">The live slice to copy.</param>
    /// <returns>A detached state containing supported patch fields.</returns>
    public static SlicePatchState FromSlice(Slice slice)
    {
        ArgumentNullException.ThrowIfNull(slice);

        return new SlicePatchState
        {
            Freq = slice.Freq, Mode = slice.DemodMode ?? string.Empty,
            IsTransmitSlice = slice.IsTransmitSlice, Active = slice.Active,
            NROn = slice.NROn, NBOn = slice.NBOn, WNBOn = slice.WNBOn,
            ANFOn = slice.ANFOn, APFOn = slice.APFOn, NrLevel = slice.NRLevel,
            NbLevel = slice.NBLevel, WnbLevel = slice.WNBLevel,
            AnfLevel = slice.ANFLevel, ApfLevel = slice.APFLevel, Mute = slice.Mute,
            AudioGain = slice.AudioGain, AudioPan = slice.AudioPan, Lock = slice.Lock
        };
    }
}
