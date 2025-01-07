using System.Collections.Generic;
using HEAppE.DomainObjects.JobManagement;

namespace HEAppE.DataAccessTier.IRepository.JobManagement.Command;

public interface ICommandTemplateRepository : IRepository<CommandTemplate>
{
    IList<CommandTemplate> GetCommandTemplatesByProjectId(long projectId);
}