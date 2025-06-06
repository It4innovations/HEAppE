﻿using System.ComponentModel;
using System.Runtime.Serialization;
using HEAppE.ExtModels.JobManagement.Models;

namespace HEAppE.ExtModels.ClusterInformation.Models;

/// <summary>
/// Cluster node type ext
/// </summary>
[DataContract(Name = "ClusterNodeTypeExt")]
[Description("Cluster node type ext")]
public class ClusterNodeTypeExt
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
    /// Number of nodes
    /// </summary>
    [DataMember(Name = "NumberOfNodes")]
    [Description("Number of nodes")]
    public int? NumberOfNodes { get; set; }

    /// <summary>
    /// Number of cores per node
    /// </summary>
    [DataMember(Name = "CoresPerNode")]
    [Description("Number of cores per node")]
    public int? CoresPerNode { get; set; }

    /// <summary>
    /// Maximum of walltime
    /// </summary>
    [DataMember(Name = "MaxWalltime")]
    [Description("Maximum of walltime")]
    public int? MaxWalltime { get; set; }

    /// <summary>
    /// File transfer id
    /// </summary>
    [DataMember(Name = "FileTransferMethodId")]
    [Description("File transfer id")]
    public long? FileTransferMethodId { get; set; }
    
    /// <summary>
    /// Queue
    /// </summary>
    [DataMember(Name = "Queue")]
    [Description("Queue")]
    public string Queue { get; set; }
    
    /// <summary>
    /// Quality of service
    /// </summary>
    [DataMember(Name = "QualityOfService")]
    [Description("Quality of service")]
    public string QualityOfService { get; set; }

    /// <summary>
    /// Cluster allocation name
    /// </summary>
    [DataMember(Name = "ClusterAllocationName")]
    [Description("Cluster allocation name")]
    public string ClusterAllocationName { get; set; }
    
    /// <summary>
    /// Cluster node type aggregation ext
    /// </summary>
    [DataMember(Name = "ClusterNodeTypeAggregation")]
    [Description("Cluster node type aggregation ext")]
    public ClusterNodeTypeAggregationExt ClusterNodeTypeAggregation { get; set; }
    
    /// <summary>
    /// Accounting
    /// </summary>
    [DataMember(Name = "Accounting")]
    [Description("Accounting")]
    public AccountingExt[] Accounting { get; set; }

    /// <summary>
    /// Array of projects
    /// </summary>
    [DataMember(Name = "Projects")]
    [Description("Array of projects")]
    public ProjectExt[] Projects { get; set; }

    public override string ToString()
    {
        return
            $"ClusterNodeTypeExt(id={Id}; name={Name}; description={Description}; numberOfNodes={NumberOfNodes}; coresPerNode={CoresPerNode}; maxWalltime={MaxWalltime}; fileTransferMethodId={FileTransferMethodId}; projects={Projects})";
    }
}