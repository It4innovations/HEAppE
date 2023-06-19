using HEAppE.DomainObjects.Management;
using HEAppE.ExtModels.ClusterInformation.Models;
using HEAppE.ExtModels.JobReporting.Models;
using HEAppE.ExtModels.Management.Models;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;

namespace HEAppE.ServiceTier.Management
{
    public interface IManagementService
    {
        CommandTemplateExt CreateCommandTemplate(long genericCommandTemplateId, string name, long projectId, string description, string code, string executableFile, string preparationScript, string sessionCode);
        PublicKeyExt CreateSecureShellKey(string username, long[] projects, string sessionCode);
        PublicKeyExt RecreateSecureShellKey(string username, string publicKey, string sessionCode);
        string RemoveSecureShellKey(string publicKey, string sessionCode);
        CommandTemplateExt ModifyCommandTemplate(long commandTemplateId, string name, long projectId, string description, string code, string executableFile, string preparationScript, string sessionCode);
        string RemoveCommandTemplate(long commandTemplateId, string sessionCode);
    }
}
