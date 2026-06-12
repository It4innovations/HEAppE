using System.Threading.Tasks;
using HEAppE.DomainObjects.JobManagement;

namespace HEAppE.DataAccessTier.IRepository.JobManagement.Command;

public interface ICommandTemplateParameterRepository : IRepository<CommandTemplateParameter>
{
    CommandTemplateParameter GetByCommandTemplateIdAndCommandParamId(long commandTemplateId, string identifier);
    Task<CommandTemplateParameter> GetByCommandTemplateIdAndCommandParamIdAsync(long commandTemplateId, string identifier);
    CommandTemplateParameter GetByIdWithCommandTemplate(long id);
    Task<CommandTemplateParameter> GetByIdWithCommandTemplateAsync(long id);
}