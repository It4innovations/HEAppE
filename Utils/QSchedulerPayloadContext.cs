using System.Collections.Generic;
using System.Threading;

namespace HEAppE.Utils;

/// <summary>
/// Thread-safe AsyncLocal context to hold QScheduler quantum circuit payloads during in-memory create-and-submit requests.
/// </summary>
public static class QSchedulerPayloadContext
{
    private static readonly AsyncLocal<Dictionary<string, byte[]>> _payloads = new();

    public static Dictionary<string, byte[]> Payloads
    {
        get => _payloads.Value;
        set => _payloads.Value = value;
    }
}
