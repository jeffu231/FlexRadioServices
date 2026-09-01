using System.Net;
using System.Net.Sockets;
using System.Reflection;
using FlexRadioServices.Models.Ports.Network;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace FlexRadioServices.Tests.Models.Ports.Network;

public sealed class TcpServerTests
{
    [Fact]
    public async Task RunAsync_ClientResetDoesNotPreventSubsequentConnections()
    {
        using var services = CreateServiceProvider();
        var server = new TcpServer(NullLogger<TcpServer>.Instance, services);
        using var stopping = new CancellationTokenSource();
        var listenerTask = server.RunAsync(IPAddress.Loopback, 0, stopping.Token);
        var endpoint = Assert.IsType<IPEndPoint>(server.LocalEndpoint);

        using (var firstClient = new TcpClient())
        {
            await firstClient.ConnectAsync(endpoint.Address, endpoint.Port);
            await WaitForClientAsync(server);

            // A non-zero linger timeout forces an RST on close, matching an abrupt
            // "connection reset by peer" disconnect instead of a graceful FIN.
            firstClient.LingerState = new LingerOption(true, 0);
        }

        await WaitForClientCountAsync(server, 0);

        using var secondClient = new TcpClient();
        await secondClient.ConnectAsync(endpoint.Address, endpoint.Port);
        await WaitForClientAsync(server);

        stopping.Cancel();
        await listenerTask.WaitAsync(TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task RunAsync_UnexpectedAcceptExceptionIsLoggedAndListenerKeepsRunning()
    {
        var logger = new CapturingLogger<TcpServer>();
        using var services = CreateServiceProvider();
        var server = new TcpServer(logger, services);
        using var stopping = new CancellationTokenSource();
        var listenerTask = server.RunAsync(IPAddress.Loopback, 0, stopping.Token);
        Assert.IsType<IPEndPoint>(server.LocalEndpoint);

        // Close the listener's underlying socket directly, without cancelling our own
        // token, to simulate AcceptTcpClientAsync failing for a reason unrelated to an
        // intentional shutdown (e.g. transient resource exhaustion).
        var listenerField = typeof(TcpServer).GetField("_listener", BindingFlags.NonPublic | BindingFlags.Instance);
        var listener = Assert.IsType<TcpListener>(listenerField!.GetValue(server));
        listener.Server.Close();

        await WaitForLogAsync(logger, LogLevel.Error, "Error accepting a connection");
        Assert.False(listenerTask.IsCompleted);

        stopping.Cancel();
        await listenerTask.WaitAsync(TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task RunAsync_CancellationStopsListenerAndClientWithinTimeout()
    {
        using var services = CreateServiceProvider();
        var server = new TcpServer(NullLogger<TcpServer>.Instance, services);
        using var stopping = new CancellationTokenSource();
        var listenerTask = server.RunAsync(IPAddress.Loopback, 0, stopping.Token);
        var endpoint = Assert.IsType<IPEndPoint>(server.LocalEndpoint);
        using var client = new TcpClient();

        await client.ConnectAsync(endpoint.Address, endpoint.Port);
        await WaitForClientAsync(server);
        stopping.Cancel();

        await listenerTask.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Empty(server.GetClients());
        Assert.Null(server.LocalEndpoint);
    }

    [Fact]
    public async Task RunAsync_PortAlreadyInUseFailsStartup()
    {
        using var reservedListener = new TcpListener(IPAddress.Loopback, 0);
        reservedListener.Start();
        var endpoint = Assert.IsType<IPEndPoint>(reservedListener.LocalEndpoint);
        using var services = CreateServiceProvider();
        var server = new TcpServer(NullLogger<TcpServer>.Instance, services);

        await Assert.ThrowsAsync<SocketException>(() => server.RunAsync(IPAddress.Loopback, endpoint.Port, CancellationToken.None));
        await server.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task Concurrency_GetClientsCanRunDuringDisconnect()
    {
        using var services = CreateServiceProvider();
        var server = new TcpServer(NullLogger<TcpServer>.Instance, services);
        using var stopping = new CancellationTokenSource();
        var listenerTask = server.RunAsync(IPAddress.Loopback, 0, stopping.Token);
        var endpoint = Assert.IsType<IPEndPoint>(server.LocalEndpoint);
        using var client = new TcpClient();

        await client.ConnectAsync(endpoint.Address, endpoint.Port);
        await WaitForClientAsync(server);
        var readers = Enumerable.Range(0, 4).Select(_ => Task.Run(() =>
        {
            for (var index = 0; index < 5_000; index++)
            {
                server.GetClients();
            }
        }));

        stopping.Cancel();
        await Task.WhenAll(readers);
        await listenerTask.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Empty(server.GetClients());
    }

    private static ServiceProvider CreateServiceProvider() => new ServiceCollection()
        .AddTransient<ITcpServerClient>(_ => new TcpServerClient(NullLogger<TcpServerClient>.Instance))
        .BuildServiceProvider();

    private static async Task WaitForClientAsync(ITcpServer server)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        while (server.GetClients().Length == 0)
        {
            await Task.Delay(10, timeout.Token);
        }
    }

    private static async Task WaitForClientCountAsync(ITcpServer server, int expectedCount)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        while (server.GetClients().Length != expectedCount)
        {
            await Task.Delay(10, timeout.Token);
        }
    }

    private static async Task WaitForLogAsync(CapturingLogger<TcpServer> logger, LogLevel level, string messageContains)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        while (!logger.Snapshot().Any(entry => entry.Level == level && entry.Message.Contains(messageContains, StringComparison.Ordinal)))
        {
            await Task.Delay(10, timeout.Token);
        }
    }

    private sealed class CapturingLogger<T> : ILogger<T>
    {
        public List<(LogLevel Level, string Message)> Entries { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            lock (Entries)
            {
                Entries.Add((logLevel, formatter(state, exception)));
            }
        }

        public (LogLevel Level, string Message)[] Snapshot()
        {
            lock (Entries)
            {
                return Entries.ToArray();
            }
        }
    }
}
