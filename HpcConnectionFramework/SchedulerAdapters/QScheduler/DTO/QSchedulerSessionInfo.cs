using System;
using System.Globalization;
using System.Text.Json;
using HEAppE.DomainObjects.UserAndLimitationManagement;

namespace HEAppE.HpcConnectionFramework.SchedulerAdapters.QScheduler.DTO;

/// <summary>
///     Session data reported by QScheduler (GET /sessions/{id} and the session open/close callback).
/// </summary>
public class QSchedulerSessionInfo
{
    public long Id { get; set; }

    /// <summary>
    ///     "waiting", "open" or "closed".
    /// </summary>
    public string State { get; set; }

    /// <summary>
    ///     QScheduler project name, i.e. with this instance's prefix.
    /// </summary>
    public string Project { get; set; }

    public DateTime? OpenedAt { get; set; }

    public DateTime? ClosedAt { get; set; }

    /// <summary>
    ///     Time the session has been open, i.e. the time charged to the project. Refreshed periodically while open.
    /// </summary>
    public long? ExecTimeMs { get; set; }

    public bool IsClosed => string.Equals(State, "closed", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    ///     Copies this consumption onto the session tracked by HEAppE. Returns true when anything changed.
    ///     Consumption only grows (late events with smaller values are ignored). Once QScheduler reports the
    ///     session closed, its consumption is final.
    /// </summary>
    public bool ApplyTo(QSchedulerSession dbSession)
    {
        if (dbSession == null)
        {
            return false;
        }

        var changed = false;
        if (OpenedAt.HasValue && dbSession.OpenedAt != OpenedAt)
        {
            dbSession.OpenedAt = OpenedAt;
            changed = true;
        }

        if (ExecTimeMs.HasValue && (dbSession.ConsumedMs == null || ExecTimeMs > dbSession.ConsumedMs))
        {
            dbSession.ConsumedMs = ExecTimeMs;
            changed = true;
        }

        if (IsClosed)
        {
            if (ClosedAt.HasValue && dbSession.ClosedAt != ClosedAt)
            {
                dbSession.ClosedAt = ClosedAt;
                changed = true;
            }

            if (dbSession.State != QSchedulerSessionState.Closed)
            {
                dbSession.State = QSchedulerSessionState.Closed;
                changed = true;
            }

            // A session closed while still waiting never opened and consumed nothing.
            if (dbSession.ConsumedMs == null)
            {
                dbSession.ConsumedMs = 0;
                changed = true;
            }

            if (!dbSession.IsConsumptionFinal)
            {
                dbSession.IsConsumptionFinal = true;
                changed = true;
            }
        }

        if (changed)
        {
            dbSession.ConsumptionUpdatedAt = DateTime.UtcNow;
        }

        return changed;
    }

    /// <summary>
    ///     Parses the session JSON. Returns null when the text is not a JSON object.
    /// </summary>
    public static QSchedulerSessionInfo Parse(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            return new QSchedulerSessionInfo
            {
                Id = GetLong(root, "id") ?? 0,
                State = GetString(root, "state")?.ToLowerInvariant(),
                Project = GetString(root, "project"),
                OpenedAt = GetDate(root, "opened_at"),
                ClosedAt = GetDate(root, "closed_at"),
                ExecTimeMs = GetLong(root, "exectime_ms")
            };
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string GetString(JsonElement root, string name)
    {
        return root.TryGetProperty(name, out var prop) && prop.ValueKind == JsonValueKind.String ? prop.GetString() : null;
    }

    private static long? GetLong(JsonElement root, string name)
    {
        if (!root.TryGetProperty(name, out var prop))
        {
            return null;
        }

        return prop.ValueKind switch
        {
            JsonValueKind.Number when prop.TryGetInt64(out var value) => value,
            JsonValueKind.String when long.TryParse(prop.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) => value,
            _ => null
        };
    }

    private static DateTime? GetDate(JsonElement root, string name)
    {
        var text = GetString(root, name);
        return DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var value)
            ? value.ToUniversalTime()
            : null;
    }
}
