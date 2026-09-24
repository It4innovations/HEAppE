using System.Net;
using System.Threading.Tasks;
using HEAppE.RestApi.IntegrationTests.Infrastructure;
using Xunit;
using FluentAssertions;

namespace HEAppE.RestApi.IntegrationTests.DataTransfer;

[Trait("Category", "Integration")]
public class DataTransferTests : IntegrationTestBase
{
    public DataTransferTests(HEAppEWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task RequestDataTransfer_InvalidTask_ReturnsError()
    {
        var sessionCode = await GetAdminSessionCodeAsync();
        var response = await _client.PostJsonAsync<object>("/heappe/DataTransfer/RequestDataTransfer", new
        {
            IpAddress = "127.0.0.1",
            Port = 8080,
            SubmittedTaskInfoId = 999999L,
            SessionCode = sessionCode
        });
        response.StatusCode.Should().NotBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task HttpGetToJobNode_InvalidTask_ReturnsError()
    {
        var sessionCode = await GetAdminSessionCodeAsync();
        var response = await _client.PostJsonAsync<object>("/heappe/DataTransfer/HttpGetToJobNode", new
        {
            HttpRequest = "/status",
            HttpHeaders = new string[0],
            SubmittedTaskInfoId = 999999L,
            NodeIPAddress = "127.0.0.1",
            NodePort = 8080,
            SessionCode = sessionCode
        });
        response.StatusCode.Should().NotBe(HttpStatusCode.OK);
    }
}
