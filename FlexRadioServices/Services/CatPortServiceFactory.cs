using FlexRadioServices.Models.Ports;
using FlexRadioServices.Models.Ports.Network;
using FlexRadioServices.Models.Settings;
using Microsoft.Extensions.DependencyInjection;

namespace FlexRadioServices.Services;

/// <summary>
/// Creates independent CAT listener services and their transient TCP servers.
/// </summary>
public sealed class CatPortServiceFactory(IServiceProvider serviceProvider) : ICatPortServiceFactory
{
    private readonly IServiceProvider _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));

    /// <inheritdoc />
    public ICatPortService Create(ResolvedCatPortBinding binding)
    {
        ArgumentNullException.ThrowIfNull(binding);

        return new FlexCatPortService(
            binding,
            _serviceProvider.GetRequiredService<ITcpServer>(),
            _serviceProvider.GetRequiredService<ILogger<FlexCatPortService>>(),
            _serviceProvider.GetRequiredService<IConnectedRadioCoordinator>());
    }
}
