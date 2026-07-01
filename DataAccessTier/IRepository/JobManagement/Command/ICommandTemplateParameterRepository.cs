using System.Collections.Generic;
using System.Threading.Tasks;
using HEAppE.DomainObjects.JobManagement;

namespace HEAppE.DataAccessTier.IRepository.JobManagement.Command;

public interface ICommandTemplateParameterRepository : IRepository<CommandTemplateParameter>
{
    CommandTemplateParameter GetByCommandTemplateIdAndCommandParamId(long commandTemplateId, string identifier);
    Task<CommandTemplateParameter> GetByCommandTemplateIdAndCommandParamIdAsync(long commandTemplateId, string identifier);
    CommandTemplateParameter GetByIdWithCommandTemplate(long id);
    Task<CommandTemplateParameter> GetByIdWithCommandTemplateAsync(long id);

    /// <summary>
    /// Batch-load all parameters for a given CommandTemplate in a single query.
    /// Use instead of per-parameter calls to avoid N+1 pattern in CompleteTaskSpecification.
    /// </summary>
    IList<CommandTemplateParameter> GetAllByCommandTemplateId(long commandTemplateId);
}