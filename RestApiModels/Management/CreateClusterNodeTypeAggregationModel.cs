using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using HEAppE.RestApiModels.AbstractModels;

namespace HEAppE.RestApiModels.Management;

/// <summary>
/// Create cluster node type aggregation model
/// </summary>
[DataContract(Name = "CreateClusterNodeTypeAggregationModel")]
[Description("Create cluster node type aggregation model")]
public class CreateClusterNodeTypeAggregationModel : SessionCodeModel
{
    /// <summary>
    /// Name
    /// </summary>
    [DataMember(Name = "Name", IsRequired = true)]
    [StringLength(50)]
    [Description("Name")]
    public string Name { get; set; }

    /// <summary>
    /// Description
    /// </summary>
    [DataMember(Name = "Description")]
    [StringLength(100)]
    [Description("Description")]
    public string Description { get; set; }

    /// <summary>
    /// Allocation type
    /// </summary>
    [DataMember(Name = "AllocationType")]
    [Description("Allocation type")]
    public string AllocationType { get; set; }

    /// <summary>
    /// Validity from
    /// </summary>
    [DataMember(Name = "ValidityFrom", IsRequired = true)]
    [Description("Validity from")]
    public DateTime ValidityFrom { get; set; }

    /// <summary>
    /// Validity to
    /// </summary>
    [DataMember(Name = "ValidityTo")]
    [Description("Validity to")]
    public DateTime? ValidityTo { get; set; }
}