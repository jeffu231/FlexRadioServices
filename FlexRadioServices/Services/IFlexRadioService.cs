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

    void DisconnectSession();
    
    bool ConnectToRadio(string serial);

    bool DisconnectRadio(string serial);
}
