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
        Assert.Empty(server.Clients);
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

    private static ServiceProvider CreateServiceProvider() => new ServiceCollection()
        .AddTransient<ITcpServerClient>(_ => new TcpServerClient(NullLogger<TcpServerClient>.Instance))
        .BuildServiceProvider();

    private static async Task WaitForClientAsync(ITcpServer server)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        while (server.Clients.Count == 0)
        {
            await Task.Delay(10, timeout.Token);
        }
    }
}
