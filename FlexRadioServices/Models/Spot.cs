namespace FlexRadioServices.Models;

/// <summary>
/// Represents a single DX Spot as provided by telnet, N1MM, or other Client
/// </summary>
public class Spot
{
    /// <summary>
    /// The frequency to be used to place the Spot.  If no TXFrequency is set, this is the assumed TX Frequency (simplex).
    /// </summary>
    public double RxFrequency { get; set; }

    /// <summary>
    /// (optional) This field would indicate a Split spot with a different 
    /// transmit frequency than the RX frequency.  When this field is
    /// blank, it is assumed to be a simplex Spot where the TX Frequency
    /// matches the RX Frequency.  Note that triggering a Spot with this
    /// field set does not automatically create a Split Slice as of v2.3.x.
    /// </summary>
    public double TxFrequency { get; set; }

    /// <summary>
    /// The Mode specified for the Spot.  Note that this may not always be provided
    /// and may not map directly to a DSPMode (e.g. SSB, PSK31, etc)
    /// </summary>
    public string Mode { get; set; } = string.Empty;

    /// <summary>
    /// The Callsign to display for the Spot (dxcall in N1MM spot packet)
    /// </summary>
    public string Callsign { get; set; } = string.Empty;

    /// <summary>
    /// (Optional). A color represented by hex.  Typical format #AARRGGBB
    /// </summary>
    public string Color { get; set; } = "ffff00";

    /// <summary>
    /// (Optional). A color represented by hex.  Typical format #AARRGGBB
    /// </summary>
    public string BackgroundColor { get; set; } = String.Empty;

    /// <summary>
    /// A string used to identify from where the Spot came.  For example, the source
    /// will be N1MM-[StationName] for spots that originate from N1MMSpot Ports.
    /// </summary>
    public string Source { get; set; } = string.Empty;

    /// <summary>
    /// The callsign of the spotter as is often reported on telnet.
    /// </summary>
    public string SpotterCallsign { get; set; } = string.Empty;

    /// <summary>
    /// The timestamp (UTC) of the spot as reported by the original source
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.Now;

    /// <summary>
    /// An expiration time.  After this many seconds, the Spot will automatically be
    /// removed from the radio.  Setting this property will reset the countdown timer.
    /// Warning: Because duplicate values are allowed to "reset" this timer, care should
    /// be taken in the client not to create a loop where an upstream PropertyChanged
    /// event triggers a downstream Property set call (which calls the radio, which
    /// generates a status message, which creates another PropertyChanged event...).
    /// </summary>
    public int LifetimeSeconds { get; set; } = 30;

    /// <summary>
    /// The Spot comment as provided by the original Spot source.
    /// </summary>
    public string Comment { get; set; } = string.Empty;

    /// <summary>
    /// The integer (1:higher-5:lower) priority of the Spot perhaps due to multipliers, etc.  Higher
    /// priority Spots will be shown lower on the Panadapter.
    /// </summary>
    public int Priority { get; set; } = 5;

    /// <summary>
    /// The action for the radio to take when a Spot is triggered (clicked in SmartSDR).
    /// The supported actions today are "tune" and "none".  The assumption is that a
    /// client that sets a Spot to "none" will likely be implementing their own
    /// functionality that tuning might interfere with.
    /// </summary>
    public string TriggerAction { get; set; } = "tune";
}