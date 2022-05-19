using HEAppE.ExtModels.ClusterInformation.Models;
using HEAppE.ExtModels.Management.Models;

namespace HEAppE.ServiceTier.Management
{
    public interface IManagementService
    {
        CommandTemplateExt CreateCommandTemplate(long genericCommandTemplateId, string name, string description, string code, string executableFile, string preparationScript, string sessionCode);

        CommandTemplateExt ModifyCommandTemplate(long commandTemplateId, string name, string description, string code, string executableFile, string preparationScript, string sessionCode);

        string RemoveCommandTemplate(long commandTemplateId, string sessionCode);

        InstanceInformationExt GetInstanceInformations(string sessionCode);
    }
}
