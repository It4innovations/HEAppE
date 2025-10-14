using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using HEAppE.ExtModels.ClusterInformation.Models;

namespace HEAppE.ExtModels.FileTransfer.Models;

/// <summary>
/// File tansfer method ext
/// </summary>
[DataContract(Name = "FileTransferMethodNoCredentialsExt")]
[Description("File tansfer method without credentials ext")]
public class FileTransferMethodNoCredentialsExt
{
    /// <summary>
    /// Id
    /// </summary>
    [DataMember(Name = "Id")]
    [Description("Id")]
    public long Id { get; set; }

    /// <summary>
    /// Server host name
    /// </summary>
    [DataMember(Name = "ServerHostname")]
    [Description("Server host name")]
    public string ServerHostname { get; set; }

    /// <summary>
    /// Shared base path
    /// </summary>
    [DataMember(Name = "SharedBasepath")]
    [StringLength(200)]
    [Description("Shared base path")]
    public string SharedBasepath { get; set; }
    
    /// <summary>
    /// Cluster id
    /// </summary>
    [DataMember(Name = "ClusterId")]
    [Description("Cluster id")]
    public long ClusterId { get; set; }
    

    /// <summary>
    /// Protocol
    /// </summary>
    [DataMember(Name = "Protocol")]
    [Description("Protocol")]
    public FileTransferProtocolExt? Protocol { get; set; }

    /// <summary>
    /// Port
    /// </summary>
    [DataMember(Name = "Port")]
    [Description("Port")]
    public int? Port { get; set; }

    public override string ToString()
    {
        return
            $"FileTransferMethodExt: ServerHostname={ServerHostname}, SharedBasepath={SharedBasepath}, Protocol={Protocol}, Port={Port}";
    }
}