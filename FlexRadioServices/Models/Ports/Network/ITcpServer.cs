using System.Net;
using System.Collections.Immutable;

namespace FlexRadioServices.Models.Ports.Network;

/// <summary>
/// Hosts TCP clients for a configured CAT port.
/// </summary>
public interface ITcpServer
{
    /// <summary>
    /// Occurs when a client connects to the listener.
    /// </summary>
    event EventHandler<ClientConnectedEventArgs> ClientConnected;

    /// <summary>
    /// Occurs when a client disconnects from the listener.
    /// </summary>
    event EventHandler<ClientDisconnectedEventArgs> ClientDisconnected;

    /// <summary>
    /// Gets a snapshot of the currently connected clients.
    /// </summary>
    ImmutableArray<RadioClientSessionSnapshot> GetClients();

    /// <summary>Sends data to every client connected at the time of the call.</summary>
    Task SendToAllAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken);

    /// <summary>
    /// Gets the listener endpoint after the listener has started.
    /// </summary>
    IPEndPoint? LocalEndpoint { get; }

    /// <summary>
    /// Gets or sets the name used to identify this port in logs.
    /// </summary>
    string PortFriendlyName { get; set; }

    /// <summary>
    /// Runs the listener until <paramref name="stoppingToken"/> is cancelled.
    /// </summary>
    /// <param name="address">The address on which to listen.</param>
    /// <param name="port">The port on which to listen.</param>
    /// <param name="stoppingToken">A token that stops the listener and its clients.</param>
    /// <returns>A task that completes when listener cleanup has finished.</returns>
    Task RunAsync(IPAddress address, int port, CancellationToken stoppingToken);

    /// <summary>
    /// Stops the listener and waits for connected clients to finish.
    /// </summary>
    /// <param name="cancellationToken">A token that cancels the wait for shutdown.</param>
    /// <returns>A task that completes when client shutdown has finished.</returns>
    Task StopAsync(CancellationToken cancellationToken);
}
