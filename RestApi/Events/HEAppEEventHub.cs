using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using HEAppE.BusinessLogicTier;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace HEAppE.RestApi.Events;

public class HEAppEEventHub : IHEAppEEventHub
{
    private readonly ConcurrentDictionary<long, ConcurrentBag<WebSocket>> _connections = new();
    private readonly ConcurrentDictionary<long, ConcurrentQueue<string>> _eventBuffers = new();
    private readonly ILogger<HEAppEEventHub> _logger;
    private const int MaxBufferSize = 50;

    public HEAppEEventHub(ILogger<HEAppEEventHub> logger)
    {
        _logger = logger;
    }

    public async Task HandleConnectionAsync(WebSocket webSocket, HttpContext httpContext, long userId)
    {
        var userConnections = _connections.GetOrAdd(userId, _ => new ConcurrentBag<WebSocket>());
        userConnections.Add(webSocket);
        _logger.LogInformation($"[EventHub] WebSocket connection established for User ID {userId}. Active connections for user: {userConnections.Count}");

        // Replay buffered events (catch-up) to prevent race conditions (e.g. client connects right after submit)
        if (_eventBuffers.TryGetValue(userId, out var buffer))
        {
            var bufferedEvents = buffer.ToArray();
            if (bufferedEvents.Length > 0)
            {
                _logger.LogInformation($"[EventHub] Replaying {bufferedEvents.Length} buffered events to new WebSocket connection for User ID {userId}.");
                foreach (var json in bufferedEvents)
                {
                    if (webSocket.State == WebSocketState.Open)
                    {
                        try
                        {
                            var bytes = Encoding.UTF8.GetBytes(json);
                            await webSocket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning($"[EventHub] Failed to replay buffered event to WebSocket for User ID {userId}: {ex.Message}");
                        }
                    }
                }
            }
        }

        var bufferBytes = new byte[1024 * 4];
        try
        {
            while (webSocket.State == WebSocketState.Open)
            {
                // We keep the connection open, reading incoming messages to detect close signals.
                var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(bufferBytes), CancellationToken.None);
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    _logger.LogInformation($"[EventHub] Received close frame from User ID {userId}.");
                    await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closed by client", CancellationToken.None);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"[EventHub] Connection error or reset for User ID {userId}: {ex.Message}");
        }
        finally
        {
            // Remove webSocket from list and prune dead sockets
            var updatedConnections = new ConcurrentBag<WebSocket>(userConnections.Where(ws => ws != webSocket && ws.State == WebSocketState.Open));
            _connections.TryUpdate(userId, updatedConnections, userConnections);
            _logger.LogInformation($"[EventHub] WebSocket connection closed for User ID {userId}. Remaining connections for user: {updatedConnections.Count}");
        }
    }

    public async Task PublishEventAsync(long userId, string eventType, string source, object data)
    {
        // Build CloudEvent payload
        var cloudEvent = new
        {
            specversion = "1.0",
            type = eventType,
            source = source,
            id = Guid.NewGuid().ToString(),
            time = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
            datacontenttype = "application/json",
            data = data
        };

        var json = JsonSerializer.Serialize(cloudEvent);

        // Buffer the event for catch-up (race-condition protection)
        var userBuffer = _eventBuffers.GetOrAdd(userId, _ => new ConcurrentQueue<string>());
        userBuffer.Enqueue(json);
        while (userBuffer.Count > MaxBufferSize)
        {
            userBuffer.TryDequeue(out _);
        }

        if (!_connections.TryGetValue(userId, out var userConnections))
        {
            return;
        }

        var activeConnections = userConnections.Where(ws => ws.State == WebSocketState.Open).ToList();
        if (!activeConnections.Any())
        {
            return;
        }

        var bytes = Encoding.UTF8.GetBytes(json);
        var segment = new ArraySegment<byte>(bytes);

        _logger.LogInformation($"[EventHub] Publishing event '{eventType}' to {activeConnections.Count} active connections of User ID {userId}.");

        var sendTasks = activeConnections.Select(async ws =>
        {
            try
            {
                await ws.SendAsync(segment, WebSocketMessageType.Text, true, CancellationToken.None);
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"[EventHub] Failed to send event to a WebSocket for User ID {userId}: {ex.Message}");
            }
        });

        await Task.WhenAll(sendTasks);
    }
}
