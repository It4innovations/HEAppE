using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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

    public async Task<CommandTemplateParameter> GetByCommandTemplateIdAndCommandParamIdAsync(long commandTemplateId, string identifier)
    {
        return await _dbSet.SingleOrDefaultAsync(w => w.CommandTemplateId == commandTemplateId && w.Identifier == identifier);
    }

    public CommandTemplateParameter GetByIdWithCommandTemplate(long id)
    {
        return _dbSet
            .Include(ctp => ctp.CommandTemplate)
            .FirstOrDefault(ctp => ctp.Id == id);
    }

    public async Task<CommandTemplateParameter> GetByIdWithCommandTemplateAsync(long id)
    {
        return await _dbSet
            .Include(ctp => ctp.CommandTemplate)
            .FirstOrDefaultAsync(ctp => ctp.Id == id);
    }

    /// <inheritdoc />
    public IList<CommandTemplateParameter> GetAllByCommandTemplateId(long commandTemplateId)
    {
        return _dbSet
            .AsNoTracking()
            .Where(p => p.CommandTemplateId == commandTemplateId)
            .ToList();
    }

    #endregion
}