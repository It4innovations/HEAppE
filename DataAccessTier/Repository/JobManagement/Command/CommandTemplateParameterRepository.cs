using System.Linq;
using HEAppE.DataAccessTier.IRepository.JobManagement.Command;
using HEAppE.DomainObjects.JobManagement;
using Microsoft.EntityFrameworkCore;

namespace HEAppE.DataAccessTier.Repository.JobManagement.Command;

internal class CommandTemplateParameterRepository : GenericRepository<CommandTemplateParameter>,
    ICommandTemplateParameterRepository
{
    #region Constructors

    internal CommandTemplateParameterRepository(MiddlewareContext context)
        : base(context)
    {
    }

    public CommandTemplateParameter GetByCommandTemplateIdAndCommandParamId(long commandTemplateId, string identifier)
    {
        return _dbSet.SingleOrDefault(w => w.CommandTemplateId == commandTemplateId && w.Identifier == identifier);
    }

    public CommandTemplateParameter GetByIdWithCommandTemplate(long id)
    {
        return _dbSet
            .Include(ctp => ctp.CommandTemplate)
            .FirstOrDefault(ctp => ctp.Id == id);
    }

    #endregion
}