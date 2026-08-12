using System.Collections.Immutable;
using FlexRadioServices.Models.Radio;

namespace FlexRadioServices.Services;

public interface IFlexRadioService
{
    /// <summary>Gets a copied snapshot of discovered radios.</summary>
    ImmutableArray<RadioSnapshot> GetDiscoveredRadios();

    /// <summary>Gets a copied snapshot of the connected radio, if any.</summary>
    RadioSnapshot? GetConnectedRadio();

    /// <summary>Gets copied snapshots of GUI clients for a radio.</summary>
    ImmutableArray<RadioClientSnapshot> GetRadioClients(string serial);

    /// <summary>Connects the discovered radio identified by <paramref name="serial"/>.</summary>
    /// <param name="serial">The serial identifier of the radio to connect.</param>
    /// <returns><see langword="true"/> if the radio was found; otherwise, <see langword="false"/>.</returns>
    bool ConnectToRadio(string serial);

    /// <summary>Disconnects the discovered radio identified by <paramref name="serial"/>.</summary>
    /// <param name="serial">The serial identifier of the radio to disconnect.</param>
    /// <returns><see langword="true"/> if the radio was found; otherwise, <see langword="false"/>.</returns>
    bool DisconnectRadio(string serial);
}
