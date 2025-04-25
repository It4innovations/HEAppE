using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using HEAppE.DomainObjects.FileTransfer;
using HEAppE.RestApiModels.AbstractModels;

namespace HEAppE.RestApiModels.Management;

/// <summary>
/// Modify file transfer method model
/// </summary>
[DataContract(Name = "ModifyFileTransferMethodModel")]
[Description("Modify file transfer method model")]
public class ModifyFileTransferMethodModel : SessionCodeModel
{
    /// <summary>
    /// Id
    /// </summary>
    [DataMember(Name = "Id", IsRequired = true)]
    [Description("Id")]
    public long Id { get; set; }

    /// <summary>
    /// Server hostname
    /// </summary>
    [DataMember(Name = "ServerHostname", IsRequired = true)]
    [StringLength(50)]
    [Description("Server hostname")]
    public string ServerHostname { get; set; }

    /// <summary>
    /// Protocol
    /// </summary>
    [DataMember(Name = "Protocol", IsRequired = true)]
    [Description("Protocol")]
    public FileTransferProtocol Protocol { get; set; }

    /// <summary>
    /// Cluster id
    /// </summary>
    [DataMember(Name = "ClusterId", IsRequired = true)]
    [Description("Cluster id")]
    public long ClusterId { get; set; }

    /// <summary>
    /// Port
    /// </summary>
    [DataMember(Name = "Port", IsRequired = false)]
    [Description("Port")]
    public int? Port { get; set; }
}