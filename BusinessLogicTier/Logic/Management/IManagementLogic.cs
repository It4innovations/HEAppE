using HEAppE.DomainObjects.JobManagement;

namespace HEAppE.BusinessLogicTier.Logic.Management
{
    public interface IManagementLogic
    {
        CommandTemplate CreateCommandTemplate(long genericCommandTemplateId, string name, string description, string code, string executableFile, string preparationScript);
        void RemoveCommandTemplate(long commandTemplateId);
        CommandTemplate ModifyCommandTemplate(long commandTemplateId, string name, string description, string code, string executableFile, string preparationScript);
    }
}
