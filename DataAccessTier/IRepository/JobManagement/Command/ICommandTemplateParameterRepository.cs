using HEAppE.DomainObjects.JobManagement;

namespace HEAppE.DataAccessTier.IRepository.JobManagement.Command
{
    public interface ICommandTemplateParameterRepository : IRepository<CommandTemplateParameter>
    {
        CommandTemplateParameter GetByCommandTemplateIdAndCommandParamId(long commandTemplateId, string identifier);
    }
}
