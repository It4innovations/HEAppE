using System.Net;
using System.Threading.Tasks;
using HEAppE.RestApi.IntegrationTests.Infrastructure;
using Xunit;
using FluentAssertions;

namespace HEAppE.RestApi.IntegrationTests.DataTransfer;

[Trait("Category", "Integration")]
public class DataTransferTests : IClassFixture<HEAppEWebApplicationFactory>
{
    private readonly ApiClient _client;

    public DataTransferTests(HEAppEWebApplicationFactory factory)
    {
        var httpClient = factory.CreateClient();
        _client = new ApiClient(httpClient);
        _client.SetApiKey("admin");
    }

    [Fact]
    public async Task RequestDataTransfer_InvalidJob_ReturnsError()
    {
        var response = await _client.PostJsonAsync<object>("/heappe/DataTransfer/RequestDataTransfer?submittedJobInfoId=-1", null!);
        response.StatusCode.Should().NotBe(HttpStatusCode.OK);
    }
}
