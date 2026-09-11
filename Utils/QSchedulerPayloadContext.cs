using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace HEAppE.Utils;

/// <summary>
/// Thread-safe AsyncLocal context to hold QScheduler quantum circuit payload streams during in-memory create-and-submit requests.
/// </summary>
public static class QSchedulerPayloadContext
{
    private static readonly AsyncLocal<Dictionary<string, Stream>> _payloads = new();

    public static Dictionary<string, Stream> Payloads
    {
        get => _payloads.Value;
        set => _payloads.Value = value;
    }
}
