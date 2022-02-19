using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HEAppE.DomainObjects.ClusterInformation
{
    public enum ClusterAuthenticationCredentialsAuthType
    {
        Password = 1,
        PasswordInteractive = 2,
        PasswordAndPrivateKey = 3,
        PrivateKey = 4,
        PrivateKeyInSshAgent = 5
    }
}
