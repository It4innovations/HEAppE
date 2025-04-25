using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using HEAppE.DomainObjects.ClusterInformation;
using HEAppE.RestApiModels.AbstractModels;

namespace HEAppE.RestApiModels.Management;

/// <summary>
/// Create cluster model
/// </summary>
[DataContract(Name = "CreateClusterModel")]
[Description("Create cluster model")]
public class CreateClusterModel : SessionCodeModel
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
    [DataMember(Name = "Description", IsRequired = false)]
    [StringLength(100)]
    [Description("Description")]
    public string Description { get; set; }

    /// <summary>
    /// Master node name
    /// </summary>
    [DataMember(Name = "MasterNodeName", IsRequired = true)]
    [StringLength(100)]
    [Description("Master node name")]
    public string MasterNodeName { get; set; }

    /// <summary>
    /// Scheduler type
    /// </summary>
    [DataMember(Name = "SchedulerType", IsRequired = true)]
    [Description("Scheduler type")]
    public SchedulerType SchedulerType { get; set; }

    /// <summary>
    /// Connection protocol
    /// </summary>
    [DataMember(Name = "ConnectionProtocol", IsRequired = true)]
    [Description("Connection protocol")]
    public ClusterConnectionProtocol ConnectionProtocol { get; set; }

    /// <summary>
    /// Time zone
    /// </summary>
    [DataMember(Name = "TimeZone", IsRequired = true)]
    [StringLength(10)]
    [Description("Time zone")]
    public string TimeZone { get; set; }

    /// <summary>
    /// Port
    /// </summary>
    [DataMember(Name = "Port", IsRequired = false)]
    [Description("Port")]
    public int? Port { get; set; }

    /// <summary>
    /// Update job state by service account
    /// </summary>
    [DataMember(Name = "UpdateJobStateByServiceAccount", IsRequired = true)]
    [Description("Update job state by service account")]
    public bool UpdateJobStateByServiceAccount { get; set; }

    /// <summary>
    /// Domain name
    /// </summary>
    [DataMember(Name = "DomainName", IsRequired = true)]
    [StringLength(20)]
    [Description("Domain name")]
    public string DomainName { get; set; }

    /// <summary>
    /// Proxy connection id
    /// </summary>
    [DataMember(Name = "ProxyConnectionId", IsRequired = true)]
    [Description("Proxy connection id")]
    public long? ProxyConnectionId { get; set; }
}