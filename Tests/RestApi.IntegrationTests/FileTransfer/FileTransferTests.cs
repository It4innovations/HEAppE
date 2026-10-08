using System.Net;
using System.Threading.Tasks;
using HEAppE.RestApi.IntegrationTests.Infrastructure;
using Xunit;
using FluentAssertions;

namespace HEAppE.RestApi.IntegrationTests.FileTransfer;

[Trait("Category", "Integration")]
public class FileTransferTests : IntegrationTestBase
{
    public FileTransferTests(HEAppEWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task RequestFileTransfer_InvalidJob_ReturnsBadRequestOrNotFound()
    {
        var sessionCode = await GetAdminSessionCodeAsync();
        var response = await _client.PostJsonAsync<object>("/heappe/FileTransfer/RequestFileTransfer", new
        {
            SubmittedJobInfoId = 999999L,
            SessionCode = sessionCode
        });
        response.StatusCode.Should().NotBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ListChangedFilesForJob_NonExistentJob_ReturnsNotFoundOrBadRequest()
    {
        var sessionCode = await GetAdminSessionCodeAsync();
        var response = await _client.GetAsync($"/heappe/FileTransfer/ListChangedFilesForJob?sessionCode={sessionCode}&submittedJobInfoId=999999");
        response.StatusCode.Should().NotBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task CloseFileTransfer_NonExistentJob_ReturnsNotFoundOrBadRequest()
    {
        var sessionCode = await GetAdminSessionCodeAsync();
        var response = await _client.PostJsonAsync<object>("/heappe/FileTransfer/CloseFileTransfer", new
        {
            SubmittedJobInfoId = 999999L,
            SessionCode = sessionCode,
            PublicKey = "ssh-rsa AAAAB3NzaC1yc2EAAAADAQABAAABAQC..."
        });
        response.StatusCode.Should().NotBe(HttpStatusCode.OK);
    }
}
