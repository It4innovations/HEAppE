using System.Collections.Generic;
using System.Threading.Tasks;
using HEAppE.DomainObjects.JobManagement;

namespace HEAppE.DataAccessTier.IRepository.JobManagement.Command;

public interface ICommandTemplateRepository : IRepository<CommandTemplate>
{
    IList<CommandTemplate> GetCommandTemplatesByProjectId(long projectId);
    Task<IList<CommandTemplate>> GetCommandTemplatesByProjectIdAsync(long projectId);

    /// <summary>
    /// Load CommandTemplates by IDs bypassing the soft-delete global query filter.
    /// Used for historical job/task data where the template may have been deleted after the job ran.
    /// </summary>
    IList<CommandTemplate> GetByIdsIncludingDeleted(IEnumerable<long> ids);
    Task<IList<CommandTemplate>> GetByIdsIncludingDeletedAsync(IEnumerable<long> ids);
}