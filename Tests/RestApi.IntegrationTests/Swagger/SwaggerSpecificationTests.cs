using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using HEAppE.RestApi.Configuration;
using HEAppE.RestApi.IntegrationTests.Infrastructure;
using Xunit;

namespace HEAppE.RestApi.IntegrationTests.Swagger;

[Trait("Category", "Integration")]
public class SwaggerSpecificationTests : IClassFixture<HEAppEWebApplicationFactory>
{
    private readonly ApiClient _client;

    public SwaggerSpecificationTests(HEAppEWebApplicationFactory factory)
    {
        var httpClient = factory.CreateClient();
        _client = new ApiClient(httpClient);
    }

    [Fact]
    public async Task SwaggerIndex_Html_ReturnsOk()
    {
        _client.ClearAuth();
        var response = await _client.GetAsync("/swagger/index.html");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("swagger-ui");
    }

    [Fact]
    public async Task SwaggerMainDocument_ReturnsValidOpenApiSpecification()
    {
        _client.ClearAuth();
        var docVersion = SwaggerConfiguration.Version;
        var response = await _client.GetAsync($"/swagger/{docVersion}/swagger.json");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        content.Should().NotBeNullOrWhiteSpace();

        using var jsonDoc = JsonDocument.Parse(content);
        var root = jsonDoc.RootElement;
        root.TryGetProperty("openapi", out var openapiProp).Should().BeTrue();
        root.TryGetProperty("paths", out var pathsProp).Should().BeTrue();

        // Verify paths include the QScheduler and ClusterInformation endpoints
        var pathsStr = pathsProp.ToString();
        pathsStr.Should().Contain("MachineArchitecture");
        pathsStr.Should().Contain("MachineCalibration");
        pathsStr.Should().Contain("MachineInfo");
    }

    [Theory]
    [InlineData("DetailedJobReporting")]
    [InlineData("Dictionary")]
    [InlineData("py4heappe")]
    public async Task SwaggerSubDocuments_ReturnValidOpenApiSpecification(string documentName)
    {
        _client.ClearAuth();
        var response = await _client.GetAsync($"/swagger/{documentName}/swagger.json");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        content.Should().NotBeNullOrWhiteSpace();

        using var jsonDoc = JsonDocument.Parse(content);
        var root = jsonDoc.RootElement;
        root.TryGetProperty("openapi", out _).Should().BeTrue();
        root.TryGetProperty("paths", out _).Should().BeTrue();
    }
}
