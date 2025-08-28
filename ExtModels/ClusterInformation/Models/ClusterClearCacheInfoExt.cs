using System;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace HEAppE.ExtModels.ClusterInformation.Models;

/// <summary>
/// Cluster ext
/// </summary>
[DataContract(Name = "ClusterClearCacheInfoExt")]
[Description("Cluster clear cache info ext")]
public class ClusterClearCacheInfoExt
{
    /// <summary>
    /// ClearedKeysCount
    /// </summary>
    [DataMember(Name = "ClearedKeysCount")]
    [Description("ClearedKeysCount")]
    public long ClearedKeysCount { get; set; }

    /// <summary>
    /// Timestamp
    /// </summary>
    [DataMember(Name = "Timestamp")]
    [Description("Timestamp")]
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Description
    /// </summary>
    [DataMember(Name = "Description")]
    [Description("Description")]
    public string Description { get; set; }

    public override string ToString()
    {
        return $"ClusterInfoExt(ClearedKeysCount={ClearedKeysCount}; Timestamp={Timestamp}; Description={Description};)";
    }
}