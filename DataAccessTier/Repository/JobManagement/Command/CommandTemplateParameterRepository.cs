using System.Linq;
using HEAppE.DataAccessTier.IRepository.JobManagement.Command;
using HEAppE.DomainObjects.JobManagement;

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
        return GetAll().SingleOrDefault(w => w.CommandTemplateId == commandTemplateId && w.Identifier == identifier);
    }

    #endregion
}