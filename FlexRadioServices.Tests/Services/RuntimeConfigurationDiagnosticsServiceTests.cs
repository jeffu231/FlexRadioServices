using FlexRadioServices.Models;
using FlexRadioServices.Models.Settings;
using FlexRadioServices.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit;

namespace FlexRadioServices.Tests.Services;

public sealed class RuntimeConfigurationDiagnosticsServiceTests
{
    [Fact]
    public async Task StartAsync_ActiveBindings_LogsConfiguredCountsAndActivePorts()
    {
        var logger = new TestLogger<RuntimeConfigurationDiagnosticsService>();
        var service = CreateService(logger, enabled: true);

        await service.StartAsync(CancellationToken.None);

        var message = Assert.Single(logger.Messages);
        Assert.Contains("configured CAT profiles: 1", message);
        Assert.Contains("configured CAT clients: 1", message);
        Assert.Contains("active CAT ports: 6101", message);
    }

    [Fact]
    public async Task StartAsync_NoActiveBindings_LogsCatAsDisabled()
    {
        var logger = new TestLogger<RuntimeConfigurationDiagnosticsService>();
        var service = CreateService(logger, enabled: false);

        await service.StartAsync(CancellationToken.None);

        Assert.Contains("active CAT ports: disabled", Assert.Single(logger.Messages));
    }

    private static RuntimeConfigurationDiagnosticsService CreateService(
        TestLogger<RuntimeConfigurationDiagnosticsService> logger,
        bool enabled)
    {
        var provider = new CatPortConfigurationProvider(Options.Create(new CatPortSettings
        {
            Profiles =
            [
                new CatPortProfileSettings
                {
                    ProfileName = "Operator",
                    PortSettings =
                    [
                        new PortSettings
                        {
                            PortFriendlyName = "CAT",
                            PortNumber = 6101,
                            PortSliceType = PortSliceType.Active
                        }
                    ]
                }
            ],
            Clients =
            [
                new CatClientSettings
                {
                    ClientId = "client-1",
                    ClientFriendlyName = "Operator Client",
                    Enabled = enabled,
                    ProfileName = "Operator"
                }
            ]
        }));

        return new RuntimeConfigurationDiagnosticsService(
            logger,
            provider,
            Options.Create(new MqttBrokerSettings()),
            Options.Create(new RadioSettings()));
    }

    private sealed class TestLogger<T> : ILogger<T>
    {
        public List<string> Messages { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            Messages.Add(formatter(state, exception));
        }
    }
}
