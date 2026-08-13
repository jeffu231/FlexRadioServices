using FlexRadioServices.Models;

namespace FlexRadioServices.Services;

internal sealed record ConnectedRadioTransition(
    RadioProxy? PreviousRadio,
    RadioProxy? CurrentRadio);
