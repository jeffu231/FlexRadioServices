using FlexRadioServices.Models.Ports;
using FlexRadioServices.Models.Settings;

namespace FlexRadioServices.Services;

/// <summary>
/// Creates CAT listener services for resolved startup bindings.
/// </summary>
public interface ICatPortServiceFactory
{
    /// <summary>
    /// Creates a CAT listener service for a resolved binding.
    /// </summary>
    /// <param name="binding">The profile, client, and port settings for the listener.</param>
    /// <returns>A CAT listener service that has not yet started.</returns>
    ICatPortService Create(ResolvedCatPortBinding binding);
}
