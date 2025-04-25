using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using HEAppE.RestApiModels.AbstractModels;

namespace HEAppE.RestApiModels.Management;

/// <summary>
/// Create sub project model
/// </summary>
[DataContract(Name = "CreateSubProjectModel")]
[Description("Create sub project model")]
public class CreateSubProjectModel : SessionCodeModel
{
    /// <summary>
    /// Identifier
    /// </summary>
    [DataMember(Name = "Identifier", IsRequired = true)]
    [StringLength(50)]
    [Description("Identifier")]
    public string Identifier { get; set; }

    /// <summary>
    /// Description
    /// </summary>
    [DataMember(Name = "Description", IsRequired = false)]
    [StringLength(100)]
    [Description("Description")]
    public string Description { get; set; }

    /// <summary>
    /// Start date
    /// </summary>
    [DataMember(Name = "StartDate", IsRequired = true)]
    [Description("Start date")]
    public DateTime StartDate { get; set; }

    /// <summary>
    /// End date
    /// </summary>
    [DataMember(Name = "EndDate", IsRequired = false)]
    [Description("End date")]
    public DateTime? EndDate { get; set; }

    /// <summary>
    /// Project id
    /// </summary>
    [DataMember(Name = "ProjectId", IsRequired = true)]
    [Description("Project id")]
    public long ProjectId { get; set; }

    public override string ToString()
    {
        return $"CreateSubProjectModel({base.ToString()}; Identifier: {Identifier}, Description: {Description}, StartDate: {StartDate}, EndDate: {EndDate}, ProjectId: {ProjectId})";
    }
}