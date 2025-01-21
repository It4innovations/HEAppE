using System;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace HEAppE.ExtModels.ClusterInformation.Models;

/// <summary>
/// Cluster node type aggregation ext
/// </summary>
[DataContract(Name = "ClusterNodeTypeAggregationExt")]
[Description("Cluster node type aggregation ext")]
public class ClusterNodeTypeAggregationExt
{
    /// <summary>
    /// Id
    /// </summary>
    [DataMember(Name = "Id")]
    [Description("Id")]
    public long? Id { get; set; }

    /// <summary>
    /// Name
    /// </summary>
    [DataMember(Name = "Name")]
    [Description("Name")]
    public string Name { get; set; }

    /// <summary>
    /// Description
    /// </summary>
    [DataMember(Name = "Description")]
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
    [DataMember(Name = "ValidityFrom")]
    [Description("Valid from")]
    public DateTime ValidityFrom { get; set; }

    /// <summary>
    /// Validity to
    /// </summary>
    [DataMember(Name = "ValidityTo")]
    [Description("Valid to")]
    public DateTime? ValidityTo { get; set; }

    public override string ToString()
    {
        return
            $"ClusterNodeTypeAggregationExt(id={Id}; name={Name}; description={Description}; allocationType={AllocationType}; validityFrom={ValidityFrom}; validityTo={ValidityTo})";
    }
}