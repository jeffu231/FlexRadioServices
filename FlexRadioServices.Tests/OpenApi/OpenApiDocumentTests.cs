using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi;
using Microsoft.OpenApi.Reader;
using Xunit;

namespace FlexRadioServices.Tests.OpenApi;

/// <summary>
/// Verifies the generated OpenAPI documents exposed by the API.
/// </summary>
public sealed class OpenApiDocumentTests(OpenApiWebApplicationFactory factory)
    : IClassFixture<OpenApiWebApplicationFactory>
{
    [Fact]
    public async Task V1JsonDocument_ReturnsValidDocumentWithExpectedPaths()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("api/frs/swagger/v1/swagger.json");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = OpenApiDocument.Parse(
            await response.Content.ReadAsStringAsync(),
            "json",
            new OpenApiReaderSettings());

        Assert.Empty(result.Diagnostic?.Errors ?? []);

        var document = result.Document ?? throw new InvalidOperationException("OpenAPI reader did not return a document.");
        Assert.Equal("1.0", document.Info.Version);
        Assert.Contains("/api/frs/v1/configuration/version", document.Paths.Keys);
        Assert.Contains("/api/frs/v1/radio/radios", document.Paths.Keys);
    }
}

/// <summary>
/// Creates an API host without vendor initialization or background workers.
/// </summary>
public sealed class OpenApiWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureServices(services => services.RemoveAll<IHostedService>());
    }
}
