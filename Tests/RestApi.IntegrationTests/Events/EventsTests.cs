using System;
using System.Net;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using HEAppE.RestApi.IntegrationTests.Infrastructure;
using Xunit;

namespace HEAppE.RestApi.IntegrationTests.Events;

[Trait("Category", "Integration")]
public class EventsTests : IntegrationTestBase
{
    public EventsTests(HEAppEWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task EventsEndpoint_WithoutWebSocketHeaders_ReturnsBadRequest()
    {
        var sessionCode = await GetAdminSessionCodeAsync();
        var response = await _client.GetAsync($"/heappe/Events?sessionCode={sessionCode}");
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task EventsEndpoint_WithWebSocketClient_ConnectsSuccessfully()
    {
        var sessionCode = await GetAdminSessionCodeAsync();
        var wsClient = _factory.Server.CreateWebSocketClient();
        var wsUri = new Uri(_factory.Server.BaseAddress, $"/heappe/Events?sessionCode={sessionCode}");

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        using var webSocket = await wsClient.ConnectAsync(wsUri, cts.Token);

        webSocket.State.Should().Be(WebSocketState.Open);
        await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", cts.Token);
    }
}
