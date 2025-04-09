using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace HEAppE.HpcConnectionFramework.SchedulerAdapters.LinuxLocal.DTO;

/// <summary>
///     Local Linux HPC Job DTOs wrapper
/// </summary>
public class LinuxLocalInfo
{
    #region Properties

    /// <summary>
    ///     Id
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    ///     SubmitTime
    /// </summary>
    public DateTime? SubmitTime { get; set; }

    /// <summary>
    ///     StartTime
    /// </summary>
    public DateTime? StartTime { get; set; }

    /// <summary>
    ///     EndTime
    /// </summary>
    public DateTime? EndTime { get; set; }

    /// <summary>
    ///     CreateTime
    /// </summary>
    public DateTime CreateTime { get; set; }

    /// <summary>
    ///     Name
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    ///     Project
    /// </summary>
    public string Project { get; set; }

    /// <summary>
    ///     Tasks
    /// </summary>
    [JsonPropertyName("Tasks")]
    public List<LinuxLocalJobDTO> Jobs { get; set; }

    #endregion
}