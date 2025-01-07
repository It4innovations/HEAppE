using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using HEAppE.DomainObjects.FileTransfer;
using HEAppE.RestApiModels.AbstractModels;

namespace HEAppE.RestApiModels.Management;

[DataContract(Name = "ModifyFileTransferMethodModel")]
public class ModifyFileTransferMethodModel : SessionCodeModel
{
    [DataMember(Name = "Id", IsRequired = true)]
    public long Id { get; set; }

    [DataMember(Name = "ServerHostname", IsRequired = true)]
    [StringLength(50)]
    public string ServerHostname { get; set; }

    [DataMember(Name = "Protocol", IsRequired = true)]
    public FileTransferProtocol Protocol { get; set; }

    [DataMember(Name = "ClusterId", IsRequired = true)]
    public long ClusterId { get; set; }

    [DataMember(Name = "Port", IsRequired = false)]
    public int? Port { get; set; }
}