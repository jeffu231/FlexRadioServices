using FlexRadioServices.Models;

namespace FlexRadioServices.Models.Configuration;

/// <summary>
/// Represents the safe listener settings stored in a CAT profile.
/// </summary>
/// <param name="PortFriendlyName">The operator-facing listener name.</param>
/// <param name="Protocol">The listener protocol.</param>
/// <param name="PortNumber">The TCP port number.</param>
/// <param name="PortSliceType">The slice-selection behavior.</param>
/// <param name="VfoASliceLetter">The slice letter assigned to VFO A.</param>
/// <param name="VfoBSliceLetter">The slice letter assigned to VFO B.</param>
/// <param name="AutoSwitchTxSlice">Whether transmit-slice changes are followed automatically.</param>
public sealed record ConfiguredPortSettingsResponse(
    string PortFriendlyName,
    string Protocol,
    ushort PortNumber,
    PortSliceType PortSliceType,
    string VfoASliceLetter,
    string VfoBSliceLetter,
    bool AutoSwitchTxSlice);
