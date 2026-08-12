using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;

namespace FlexRadioServices.Models.Ports.Network;

/// <summary>
/// Accepts TCP connections and owns their cancellation-aware lifetimes.
/// </summary>
public sealed class TcpServer(ILogger<TcpServer> logger, IServiceProvider serviceProvider) : ITcpServer
{
    private readonly object _lifecycleLock = new();
    private readonly ConcurrentDictionary<ITcpServerClient, byte> _clients = [];
    private readonly List<Task> _clientTasks = [];
    private TcpListener? _listener;
    private CancellationTokenSource? _clientCancellation;

    /// <inheritdoc/>
    public event EventHandler<ClientConnectedEventArgs>? ClientConnected;

    /// <inheritdoc/>
    public event EventHandler<ClientDisconnectedEventArgs>? ClientDisconnected;

    /// <inheritdoc/>
    public IReadOnlyCollection<ITcpServerClient> Clients => _clients.Keys.ToArray();

    /// <inheritdoc/>
    public IPEndPoint? LocalEndpoint { get; private set; }

    /// <inheritdoc/>
    public string PortFriendlyName { get; set; } = string.Empty;

    /// <inheritdoc/>
    public async Task RunAsync(IPAddress address, int port, CancellationToken stoppingToken)
    {
        ArgumentNullException.ThrowIfNull(address);

        using var clientCancellation = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
        var listener = StartListener(address, port, clientCancellation);
        try
        {
            while (!clientCancellation.IsCancellationRequested)
            {
                TcpClient acceptedClient;
                try
                {
                    logger.LogDebug("Waiting for a connection on port {Port}", port);
                    acceptedClient = await listener.AcceptTcpClientAsync(clientCancellation.Token).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (clientCancellation.IsCancellationRequested)
                {
                    break;
                }
                catch (SocketException exception) when (clientCancellation.IsCancellationRequested)
                {
                    logger.LogDebug(exception, "Listener on port {Port} stopped while accepting", port);
                    break;
                }

                var client = serviceProvider.GetRequiredService<ITcpServerClient>();
                client.Client = acceptedClient;
                client.ConnectionClosed += ClientOnConnectionClosed;
                _clients.TryAdd(client, 0);
                ClientConnected?.Invoke(this, new ClientConnectedEventArgs(client));
                var clientTask = RunClientAsync(client, clientCancellation.Token);
                lock (_lifecycleLock)
                {
                    _clientTasks.Add(clientTask);
                }
                logger.LogDebug("Client connected on port {Port}", port);
            }
        }
        finally
        {
            listener.Stop();
            // ReSharper disable once MethodHasAsyncOverload
            // That cancellation is part of RunAsync’s synchronous finally path; it only signals the token.
            // The following await StopClientsAsync() already waits for every client read loop to observe cancellation and complete.
            // CancelAsync() would add no correctness benefit and could complicate exception flow from cancellation callbacks.
            clientCancellation.Cancel();
            await StopClientsAsync().ConfigureAwait(false);
            lock (_lifecycleLock)
            {
                if (ReferenceEquals(_listener, listener))
                {
                    _listener = null;
                    _clientCancellation = null;
                    LocalEndpoint = null;
                    _clientTasks.Clear();
                }
            }

            logger.LogDebug("Listener on port {Port} stopped", port);
        }
    }

    /// <inheritdoc/>
    public Task StopAsync(CancellationToken cancellationToken)
    {
        lock (_lifecycleLock)
        {
            _listener?.Stop();
            _clientCancellation?.Cancel();
        }

        return StopClientsAsync().WaitAsync(cancellationToken);
    }

    private TcpListener StartListener(IPAddress address, int port, CancellationTokenSource clientCancellation)
    {
        lock (_lifecycleLock)
        {
            if (_listener is not null)
            {
                throw new InvalidOperationException("The TCP listener is already running.");
            }

            logger.LogDebug("Starting listener {PortName} on {Address} and port {Port}", PortFriendlyName, address, port);
            var listener = new TcpListener(address, port);
            listener.Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            listener.Start();
            _listener = listener;
            _clientCancellation = clientCancellation;
            LocalEndpoint = (IPEndPoint)listener.LocalEndpoint;
            return listener;
        }
    }

    private async Task RunClientAsync(ITcpServerClient client, CancellationToken cancellationToken)
    {
        try
        {
            await client.RunAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            RemoveClient(client);
        }
    }

    private async Task StopClientsAsync()
    {
        foreach (var client in Clients)
        {
            client.Stop();
        }

        Task[] clientTasks;
        lock (_lifecycleLock)
        {
            clientTasks = _clientTasks.ToArray();
        }
        if (clientTasks.Length > 0)
        {
            await Task.WhenAll(clientTasks).ConfigureAwait(false);
        }
    }

    private void ClientOnConnectionClosed(object? sender, EventArgs eventArgs)
    {
        if (sender is ITcpServerClient client)
        {
            RemoveClient(client);
        }
    }

    private void RemoveClient(ITcpServerClient client)
    {
        client.ConnectionClosed -= ClientOnConnectionClosed;
        if (_clients.TryRemove(client, out _))
        {
            ClientDisconnected?.Invoke(this, new ClientDisconnectedEventArgs(client));
        }
    }
}
