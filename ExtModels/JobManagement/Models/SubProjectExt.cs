using System;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace HEAppE.ExtModels.JobManagement.Models;

/// <summary>
/// Sub project ext
/// </summary>
[DataContract(Name = "SubProjectExt")]
[Description("Sub project ext")]
public class SubProjectExt
{
    /// <summary>
    /// Id
    /// </summary>
    [DataMember(Name = "Id")]
    [Description("Id")]
    public long Id { get; set; }

    /// <summary>
    /// Identifier
    /// </summary>
    [DataMember(Name = "Identifier")]
    [Description("Identifier")]
    public string Identifier { get; set; }

    /// <summary>
    /// Description
    /// </summary>
    [DataMember(Name = "Description")]
    [Description("Description")]
    public string Description { get; set; }

    /// <summary>
    /// Start date
    /// </summary>
    [DataMember(Name = "StartDate")]
    [Description("Start date")]
    public DateTime StartDate { get; set; }

    /// <summary>
    /// End date
    /// </summary>
    [DataMember(Name = "EndDate")]
    [Description("End date")]
    public DateTime? EndDate { get; set; }

    /// <summary>
    /// Project id
    /// </summary>
    [DataMember(Name = "ProjectId")]
    [Description("Project id")]
    public long ProjectId { get; set; }

    public override string ToString()
    {
        return $"SubProjectExt(Id={Id}; Identifier={Identifier}; Description={Description}; StartDate={StartDate}; EndDate={EndDate}; ProjectId={ProjectId})";
    }
}