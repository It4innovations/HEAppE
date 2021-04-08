using System;

namespace HEAppE.DomainObjects.ClusterInformation
{
    [Flags]
    public enum ClusterConnectionProtocol
    {
        MicrosoftHpcApi = 1,
        Ssh = 2,
        SshInteractive = 4
    }
}