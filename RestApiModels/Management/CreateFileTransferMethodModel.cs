using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using HEAppE.DomainObjects.FileTransfer;
using HEAppE.RestApiModels.AbstractModels;

namespace HEAppE.RestApiModels.Management;

/// <summary>
/// Create file transfer method model
/// </summary>
[DataContract(Name = "CreateFileTransferMethodModel")]
[Description("Create file transfer method model")]
public class CreateFileTransferMethodModel : SessionCodeModel
{
    /// <summary>
    /// Server host name
    /// </summary>
    [DataMember(Name = "ServerHostname", IsRequired = true)]
    [StringLength(50)]
    [Description("Server host name")]
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