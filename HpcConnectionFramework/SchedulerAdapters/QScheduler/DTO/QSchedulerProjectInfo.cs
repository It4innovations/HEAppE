using System.Collections.Generic;
using System.Text.Json;

namespace HEAppE.HpcConnectionFramework.SchedulerAdapters.QScheduler.DTO;

/// <summary>
///     Project as reported by QScheduler (GET /projects, GET /projects/{name}).
/// </summary>
public class QSchedulerProjectInfo
{
    /// <summary>
    ///     QScheduler project name, i.e. "&lt;prefix&gt;.&lt;AccountingString&gt;" for HEAppE projects.
    /// </summary>
    public string Name { get; set; }

    public long ConsumedMs { get; set; }

    public long LimitMs { get; set; }

    public bool Active { get; set; }

    /// <summary>
    ///     Parses the GET /projects response (a JSON array). Returns an empty list for anything else.
    /// </summary>
    public static IList<QSchedulerProjectInfo> ParseList(string json)
    {
        var result = new List<QSchedulerProjectInfo>();
        if (string.IsNullOrWhiteSpace(json))
        {
            return result;
        }

        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.ValueKind != JsonValueKind.Array)
            {
                return result;
            }

            foreach (var item in doc.RootElement.EnumerateArray())
            {
                if (item.ValueKind != JsonValueKind.Object ||
                    !item.TryGetProperty("name", out var name) || name.ValueKind != JsonValueKind.String)
                {
                    continue;
                }

                result.Add(new QSchedulerProjectInfo
                {
                    Name = name.GetString(),
                    ConsumedMs = item.TryGetProperty("consumed_ms", out var consumed) && consumed.TryGetInt64(out var c) ? c : 0,
                    LimitMs = item.TryGetProperty("limit_ms", out var limit) && limit.TryGetInt64(out var l) ? l : 0,
                    Active = item.TryGetProperty("active", out var active) && active.ValueKind == JsonValueKind.True
                });
            }
        }
        catch (JsonException)
        {
            // Not JSON: treat as no projects.
        }

        return result;
    }
}
