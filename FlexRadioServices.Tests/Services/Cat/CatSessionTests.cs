using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using FlexRadioServices.Models.Ports.Network;
using FlexRadioServices.Services;
using FlexRadioServices.Services.Cat;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace FlexRadioServices.Tests.Services.Cat;

public sealed class CatSessionTests
{
    [Fact]
    public async Task TcpClients_KeepInterleavedLoopbackFramesSeparate()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var endpoint = Assert.IsType<IPEndPoint>(listener.LocalEndpoint);
        using var clientA = new TcpClient();
        using var clientB = new TcpClient();
        var connectA = clientA.ConnectAsync(IPAddress.Loopback, endpoint.Port);
        var connectB = clientB.ConnectAsync(IPAddress.Loopback, endpoint.Port);
        using var acceptedA = await listener.AcceptTcpClientAsync();
        using var acceptedB = await listener.AcceptTcpClientAsync();
        await Task.WhenAll(connectA, connectB);

        var sink = new RecordingCommandSink();
        var serverA = new TcpServerClient(NullLogger<TcpServerClient>.Instance) { Client = acceptedA };
        var serverB = new TcpServerClient(NullLogger<TcpServerClient>.Instance) { Client = acceptedB };
        var sessionA = new CatSession(serverA, sink, NullLogger.Instance);
        var sessionB = new CatSession(serverB, sink, NullLogger.Instance);
        serverA.DataReceived += (_, bytes, token) => sessionA.ProcessAsync(bytes, token);
        serverB.DataReceived += (_, bytes, token) => sessionB.ProcessAsync(bytes, token);
        var readA = serverA.RunAsync(CancellationToken.None);
        var readB = serverB.RunAsync(CancellationToken.None);

        await clientA.GetStream().WriteAsync(Encoding.ASCII.GetBytes("I"));
        await clientB.GetStream().WriteAsync(Encoding.ASCII.GetBytes("ID;"));
        await clientA.GetStream().WriteAsync(Encoding.ASCII.GetBytes("F;"));
        await WaitForCommandCountAsync(sink, 2);

        Assert.Contains(sink.Commands, command => command.Command == "ID" && command.Session == sessionB);
        Assert.Contains(sink.Commands, command => command.Command == "IF" && command.Session == sessionA);
        serverA.Stop();
        serverB.Stop();
        await Task.WhenAll(readA, readB);
    }
    [Fact]
    public async Task ProcessAsync_KeepsInterleavedClientFragmentsSeparate()
    {
        var sink = new RecordingCommandSink();
        var clientA = new TestTcpServerClient("127.0.0.1:10001");
        var clientB = new TestTcpServerClient("127.0.0.1:10002");
        var sessionA = new CatSession(clientA, sink, NullLogger.Instance);
        var sessionB = new CatSession(clientB, sink, NullLogger.Instance);

        await sessionA.ProcessAsync(Encoding.ASCII.GetBytes("I"), CancellationToken.None);
        await sessionB.ProcessAsync(Encoding.ASCII.GetBytes("ID;"), CancellationToken.None);
        await sessionA.ProcessAsync(Encoding.ASCII.GetBytes("F;FT0;"), CancellationToken.None);

        Assert.Collection(sink.Commands,
            command => Assert.Equal(("ID", sessionB), (command.Command, command.Session)),
            command => Assert.Equal(("IF", sessionA), (command.Command, command.Session)),
            command => Assert.Equal(("FT0", sessionA), (command.Command, command.Session)));
    }

    [Fact]
    public async Task ProcessAsync_ClosesOnlyClientWithOversizedIncompleteFrame()
    {
        var sink = new RecordingCommandSink();
        var rejectedClient = new TestTcpServerClient("127.0.0.1:10001");
        var usableClient = new TestTcpServerClient("127.0.0.1:10002");
        var rejectedSession = new CatSession(rejectedClient, sink, NullLogger.Instance);
        var usableSession = new CatSession(usableClient, sink, NullLogger.Instance);

        await rejectedSession.ProcessAsync(new byte[CatSession.MaxCatCommandLength + 1], CancellationToken.None);
        await usableSession.ProcessAsync(Encoding.ASCII.GetBytes("ID;"), CancellationToken.None);

        Assert.True(rejectedClient.Stopped);
        Assert.False(usableClient.Stopped);
        var command = Assert.Single(sink.Commands);
        Assert.Equal("ID", command.Command);
        Assert.Same(usableSession, command.Session);
    }

    [Fact]
    public async Task ProcessAsync_WaitsForBoundedSinkAdmission()
    {
        var sink = new BlockingCommandSink();
        var session = new CatSession(new TestTcpServerClient("127.0.0.1:10001"), sink, NullLogger.Instance);

        var admission = session.ProcessAsync(Encoding.ASCII.GetBytes("ID;"), CancellationToken.None).AsTask();

        Assert.False(admission.IsCompleted);
        sink.AllowAdmission();
        await admission;
    }

    private sealed class RecordingCommandSink : ICatCommandSink
    {
        public ConcurrentQueue<CatCommand> Commands { get; } = [];

        public ValueTask EnqueueAsync(CatCommand command, CancellationToken cancellationToken)
        {
            Commands.Enqueue(command);
            return ValueTask.CompletedTask;
        }
    }

    private static async Task WaitForCommandCountAsync(RecordingCommandSink sink, int count)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        while (sink.Commands.Count < count)
        {
            await Task.Delay(10, timeout.Token);
        }
    }

    private sealed class BlockingCommandSink : ICatCommandSink
    {
        private readonly TaskCompletionSource _admission = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public ValueTask EnqueueAsync(CatCommand command, CancellationToken cancellationToken) => new(_admission.Task.WaitAsync(cancellationToken));

        public void AllowAdmission() => _admission.SetResult();
    }

    private sealed class TestTcpServerClient(string remoteEndPoint) : ITcpServerClient
    {
        public event Func<ITcpServerClient, ReadOnlyMemory<byte>, CancellationToken, ValueTask>? DataReceived
        {
            add { }
            remove { }
        }
        public event EventHandler<EventArgs>? ConnectionClosed;
        public TcpClient? Client { get; set; }
        public bool Connected => !Stopped;
        public string RemoteEndPoint { get; } = remoteEndPoint;
        public bool Stopped { get; private set; }

        public ValueTask SendAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken) => ValueTask.CompletedTask;

        public Task RunAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        public void Stop()
        {
            Stopped = true;
            ConnectionClosed?.Invoke(this, EventArgs.Empty);
        }
    }
}
