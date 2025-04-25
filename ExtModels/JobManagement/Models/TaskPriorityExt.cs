using System.ComponentModel;

namespace HEAppE.ExtModels.JobManagement.Models;

/// <summary>
/// Task priority types
/// </summary>
[Description("Task priority types")]
public enum TaskPriorityExt
{
    Lowest,
    VeryLow,
    Low,
    BelowAverage,
    Average,
    AboveAverage,
    High,
    VeryHigh,
    Critical
}