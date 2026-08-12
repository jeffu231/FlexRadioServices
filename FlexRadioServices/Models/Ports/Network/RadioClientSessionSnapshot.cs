namespace FlexRadioServices.Models.Ports.Network;

/// <summary>
/// An immutable diagnostic view of a connected TCP client session.
/// </summary>
public sealed record RadioClientSessionSnapshot(string RemoteEndpoint);
