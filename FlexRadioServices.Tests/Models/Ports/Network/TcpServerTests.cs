using System.Net;
using System.Net.Sockets;
using FlexRadioServices.Models.Ports.Network;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace FlexRadioServices.Tests.Models.Ports.Network;

public sealed class TcpServerTests
{
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
}
