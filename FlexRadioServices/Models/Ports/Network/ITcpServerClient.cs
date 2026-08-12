using System.Net.Sockets;

namespace FlexRadioServices.Models.Ports.Network;

/// <summary>
/// Represents one client connected to a TCP listener.
/// </summary>
public interface ITcpServerClient
{
    /// <summary>
    /// Occurs when bytes are received from the client.
    /// </summary>
    event Func<ITcpServerClient, ReadOnlyMemory<byte>, CancellationToken, ValueTask>? DataReceived;

    /// <summary>
    /// Occurs once when the client connection closes.
    /// </summary>
    event EventHandler<EventArgs>? ConnectionClosed;

    /// <summary>
    /// Gets a value that indicates whether the client remains connected.
    /// </summary>
    bool Connected { get; }

    /// <summary>
    /// Gets the remote endpoint for this client.
    /// </summary>
    string RemoteEndPoint { get; }

    /// <summary>
    /// Gets or sets the accepted TCP client.
    /// </summary>
    TcpClient? Client { get; set; }

    /// <summary>
    /// Reads incoming data until the connection closes or <paramref name="cancellationToken"/> is cancelled.
    /// </summary>
    /// <param name="cancellationToken">A token that stops reading.</param>
    /// <returns>A task that completes when the client closes.</returns>
    Task RunAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Sends bytes to the client.
    /// </summary>
    /// <param name="data">The bytes to send.</param>
    /// <param name="cancellationToken">A token that cancels the send operation.</param>
    /// <returns>A task that completes when the bytes are sent.</returns>
    ValueTask SendAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken);

    /// <summary>
    /// Stops the client connection. This operation is idempotent.
    /// </summary>
    void Stop();
}
