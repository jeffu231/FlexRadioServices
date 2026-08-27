namespace FlexRadioServices.Models.Settings;

/// <summary>
/// Represents one CAT TCP listener and its slice-selection behavior.
/// </summary>
public sealed record PortSettings
{
    /// <summary>
    /// Gets or sets the operator-facing name of the CAT listener.
    /// </summary>
    public string PortFriendlyName { get; init; } = "Not Named";
    
    /// <summary>
    /// Gets or sets the listener protocol. Only TCP is supported.
    /// </summary>
    public string Protocol { get; init; } = "TCP";

    /// <summary>
    /// Gets or sets the TCP port on which the listener accepts connections.
    /// </summary>
    public ushort PortNumber { get; init; }

    /// <summary>
    /// Gets or sets the slice-selection behavior for the listener.
    /// </summary>
    public PortSliceType PortSliceType { get; init; }

    /// <summary>
    /// Gets or sets the slice letter assigned to VFO A.
    /// </summary>
    public string VfoASliceLetter { get; init; } = "A";
    
    /// <summary>
    /// Gets or sets the optional slice letter assigned to VFO B.
    /// </summary>
    public string VfoBSliceLetter { get; init; } = "A";
    
    /// <summary>
    /// Gets or sets a value that indicates whether transmit-slice changes are followed automatically.
    /// </summary>
    public bool AutoSwitchTxSlice { get; init; }
    
}
