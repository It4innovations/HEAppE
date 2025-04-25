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
    MicrosoftHpcApi = 1,
    Ssh = 2,
    SshInteractive = 4
}