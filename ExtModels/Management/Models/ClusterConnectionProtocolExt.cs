using System.ComponentModel;
using System.Runtime.Serialization;

namespace HEAppE.ExtModels.Management.Models;

/// <summary>
/// Scheduler type ext
/// </summary>
[DataContract(Name = "ClusterConnectionProtocolExt")]
[Description("ClusterConnectionProtocol ext")]
public enum ClusterConnectionProtocolExt
{
    None = 0,
    MicrosoftHpcApi = 1,
    Ssh = 2,
    SshInteractive = 4,
    Https = 8,
    Http = 16
}