using System.ComponentModel;

namespace HEAppE.ExtModels.Management.Models;

/// <summary>
/// Accounting state types
/// </summary>
[Description("Accounting state types")]
public enum AccountingStateTypeExt
{
    Unknown = 0,
    Queued = 1,
    Running = 2,
    Finished = 4,
    Failed = 8
}