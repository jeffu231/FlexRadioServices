using FlexRadioServices.Services.Cat;

namespace FlexRadioServices.Services;

/// <summary>
/// Represents a complete CAT command and the session that submitted it.
/// </summary>
internal sealed record CatCommand(string Command, CatSession Session);
