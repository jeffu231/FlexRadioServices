using System.Net;
using System.Net.Sockets;
using System.Text;

namespace FlexRadioServices.Models.Ports.Network;

/// <summary>
/// Implements the read and write lifetime for an accepted TCP client.
/// </summary>
public sealed class TcpServerClient(ILogger<TcpServerClient> logger) : ITcpServerClient
{
    private readonly SemaphoreSlim _writeLock = new(1, 1);
    private string _remoteEndPoint = string.Empty;
    private int _port;
    private int _stopped;

    /// <inheritdoc/>
    public event Func<ITcpServerClient, ReadOnlyMemory<byte>, CancellationToken, ValueTask>? DataReceived;

    /// <inheritdoc/>
    public event EventHandler<EventArgs>? ConnectionClosed;

    /// <inheritdoc/>
    public TcpClient? Client { get; set; }

    /// <inheritdoc/>
    public bool Connected => Volatile.Read(ref _stopped) == 0 && Client?.Connected == true;

    /// <inheritdoc/>
    public string RemoteEndPoint => _remoteEndPoint;

    /// <inheritdoc/>
    public async Task RunAsync(CancellationToken cancellationToken)
    {
        var client = Client ?? throw new InvalidOperationException("A TCP client must be assigned before it can run.");
        SetEndpoints(client);
        logger.LogInformation("Starting client {ClientIpAddress} on port {Port}", _remoteEndPoint, _port);

        try
        {
            var stream = client.GetStream();
            var buffer = new byte[256];
            while (client.Connected)
            {
                var bytesRead = await stream.ReadAsync(buffer.AsMemory(), cancellationToken).ConfigureAwait(false);
                if (bytesRead == 0)
                {
                    break;
                }

                await OnDataReceivedAsync(buffer.AsMemory(0, bytesRead), cancellationToken).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            logger.LogDebug("Reading client {ClientIpAddress} was cancelled", _remoteEndPoint);
        }
        catch (IOException exception) when (Volatile.Read(ref _stopped) != 0)
        {
            logger.LogDebug(exception, "Client {ClientIpAddress} closed while reading", _remoteEndPoint);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Exception reading from client {ClientIpAddress}", _remoteEndPoint);
        }
        finally
        {
            Stop();
        }
    }

    /// <inheritdoc/>
    public async ValueTask SendAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken)
    {
        if (data.IsEmpty || !Connected || Client is not { } client)
        {
            return;
        }

        await _writeLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (Connected)
            {
                await client.GetStream().WriteAsync(data, cancellationToken).ConfigureAwait(false);
            }
        }
        finally
        {
            _writeLock.Release();
        }
    }

    /// <summary>
    /// Sends an ASCII CAT response to the client.
    /// </summary>
    /// <param name="message">The response to send.</param>
    /// <param name="cancellationToken">A token that cancels the send operation.</param>
    /// <returns>A task that completes when the response is sent.</returns>
    public ValueTask SendAsync(string message, CancellationToken cancellationToken) =>
        string.IsNullOrEmpty(message)
            ? ValueTask.CompletedTask
            : SendAsync(Encoding.ASCII.GetBytes(message), cancellationToken);

    /// <inheritdoc/>
    public void Stop()
    {
        if (Interlocked.Exchange(ref _stopped, 1) != 0)
        {
            return;
        }

        Client?.Close();
        ConnectionClosed?.Invoke(this, EventArgs.Empty);
        logger.LogInformation("Client {ClientIpAddress} on port {Port} stopped", _remoteEndPoint, _port);
    }

    /// <inheritdoc/>
    public override string ToString() => $"Client {_remoteEndPoint} on port {_port}";

    private void SetEndpoints(TcpClient client)
    {
        if (client.Client.RemoteEndPoint is IPEndPoint remoteEndpoint)
        {
            _remoteEndPoint = remoteEndpoint.ToString();
        }

        if (client.Client.LocalEndPoint is IPEndPoint localEndpoint)
        {
            _port = localEndpoint.Port;
        }
    }

    private async ValueTask OnDataReceivedAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken)
    {
        if (DataReceived is null)
        {
            return;
        }

        foreach (var handler in DataReceived.GetInvocationList().Cast<Func<ITcpServerClient, ReadOnlyMemory<byte>, CancellationToken, ValueTask>>())
        {
            await handler(this, data, cancellationToken).ConfigureAwait(false);
        }
    }
}
