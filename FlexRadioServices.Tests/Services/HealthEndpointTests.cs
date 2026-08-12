using System.Net;
using FlexRadioServices.Tests.OpenApi;
using Xunit;

namespace FlexRadioServices.Tests.Services;

/// <summary>
/// Verifies the live and ready health endpoint contracts.
/// </summary>
public sealed class HealthEndpointTests(OpenApiWebApplicationFactory factory)
    : IClassFixture<OpenApiWebApplicationFactory>
{
    [Fact]
    public async Task HealthEndpoints_ReportLiveAndUnreadyWithoutFlexLibStartup()
    {
        var client = factory.CreateClient();

        var liveResponse = await client.GetAsync("/health/live");
        var readyResponse = await client.GetAsync("/health/ready");

        Assert.Equal(HttpStatusCode.OK, liveResponse.StatusCode);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, readyResponse.StatusCode);
    }
}
