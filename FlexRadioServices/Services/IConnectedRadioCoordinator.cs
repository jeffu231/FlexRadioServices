using FlexRadioServices.Models;

namespace FlexRadioServices.Services;

internal interface IConnectedRadioCoordinator
{
    event EventHandler<ConnectedRadioTransition>? ConnectedRadioChanged;

    RadioProxy? GetConnectedRadioHandle();
}
