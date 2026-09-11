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
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

using HEAppE.ExtModels.Events.Models;

namespace HEAppE.RestApi.Events;


public class HEAppEEventHub : IHEAppEEventHub
{
    private readonly ConcurrentDictionary<long, ConcurrentBag<WebSocket>> _connections = new();
    private readonly IMemoryCache _memoryCache;
    private readonly ILogger<HEAppEEventHub> _logger;
    private const int MaxBufferSize = 50;

    public HEAppEEventHub(IMemoryCache memoryCache, ILogger<HEAppEEventHub> logger)
    {
        _memoryCache = memoryCache;
        _logger = logger;
    }

    public async Task HandleConnectionAsync(WebSocket webSocket, HttpContext httpContext, long userId)
    {
        var userConnections = _connections.GetOrAdd(userId, _ => new ConcurrentBag<WebSocket>());
        userConnections.Add(webSocket);
        _logger.LogInformation($"[EventHub] WebSocket connection established for User ID {userId}. Active connections for user: {userConnections.Count}");

        // Replay buffered events (catch-up) to prevent race conditions (e.g. client connects right after submit)
        var cacheKey = $"EventBuffer:{userId}";
        if (_memoryCache.TryGetValue(cacheKey, out ConcurrentQueue<string>? buffer) && buffer != null)
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
        var cloudEvent = new CloudEventExt
        {
            SpecVersion = "1.0",
            Type = eventType,
            Source = source,
            Id = Guid.NewGuid().ToString(),
            Time = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
            DataContentType = "application/json",
            Data = data
        };

        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
        var json = JsonSerializer.Serialize(cloudEvent, options);

        // Buffer the event for catch-up (race-condition protection) with sliding expiration of 15 minutes
        var cacheKey = $"EventBuffer:{userId}";
        if (!_memoryCache.TryGetValue(cacheKey, out ConcurrentQueue<string>? userBuffer) || userBuffer == null)
        {
            userBuffer = new ConcurrentQueue<string>();
            var cacheEntryOptions = new MemoryCacheEntryOptions()
                .SetSlidingExpiration(TimeSpan.FromMinutes(15));
            _memoryCache.Set(cacheKey, userBuffer, cacheEntryOptions);
        }
        else
        {
            // Reset the cache entry to refresh sliding expiration on write
            var cacheEntryOptions = new MemoryCacheEntryOptions()
                .SetSlidingExpiration(TimeSpan.FromMinutes(15));
            _memoryCache.Set(cacheKey, userBuffer, cacheEntryOptions);
        }

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
