using System.Net;
using System.Threading.Tasks;
using HEAppE.RestApi.IntegrationTests.Infrastructure;
using Xunit;
using FluentAssertions;

namespace HEAppE.RestApi.IntegrationTests.FileTransfer;

[Trait("Category", "Integration")]
public class FileTransferTests : IClassFixture<HEAppEWebApplicationFactory>
{
    private readonly ApiClient _client;

    public FileTransferTests(HEAppEWebApplicationFactory factory)
    {
        var httpClient = factory.CreateClient();
        _client = new ApiClient(httpClient);
        _client.SetApiKey("admin");
    }

    [Fact]
    public async Task RequestFileTransfer_InvalidJob_ReturnsBadRequestOrNotFound()
    {
        var response = await _client.PostJsonAsync<object>("/heappe/FileTransfer/RequestFileTransfer?submittedJobInfoId=-1", null);
        response.StatusCode.Should().NotBe(HttpStatusCode.OK);
    }
}
