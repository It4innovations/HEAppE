using HEAppE.DomainObjects.JobManagement;
using System.Collections.Generic;

namespace HEAppE.DataAccessTier.IRepository.JobManagement.Command
{
    public interface ICommandTemplateRepository : IRepository<CommandTemplate>
    {
        IList<CommandTemplate> GetCommandTemplatesByProjectId(long projectId);
    }

}