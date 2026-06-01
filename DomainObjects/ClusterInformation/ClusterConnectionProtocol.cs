using System;

namespace HEAppE.DomainObjects.ClusterInformation;

[Flags]
public enum ClusterConnectionProtocol
{
    None = 0,
    MicrosoftHpcApi = 1,
    Ssh = 2,
    SshInteractive = 4,
    FirecRestApi = 16
}