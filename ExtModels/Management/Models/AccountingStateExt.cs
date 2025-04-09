using System;
using System.ComponentModel;

namespace HEAppE.ExtModels.Management.Models;

/// <summary>
/// Accounting state ext
/// </summary>
[Description("Accounting state ext")]
public class AccountingStateExt
{
    /// <summary>
    /// Project id
    /// </summary>
    [Description("Project id")] 
    public long ProjectId { get; set; }

    /// <summary>
    /// State
    /// </summary>
    [Description("State")] 
    public AccountingStateTypeExt State { get; set; }

    /// <summary>
    /// Comuputing start date
    /// </summary>
    [Description("Comuputing start date")] 
    public DateTime ComputingStartDate { get; set; }

    /// <summary>
    /// Comuputing end date
    /// </summary>
    [Description("Comuputing end date")] 
    public DateTime? ComputingEndDate { get; set; }

    /// <summary>
    /// Triggered at
    /// </summary>
    [Description("Triggered at")] 
    public DateTime TriggeredAt { get; set; }

    /// <summary>
    /// Last updated at
    /// </summary>
    [Description("Last updated at")] 
    public DateTime? LastUpdatedAt { get; set; }
}