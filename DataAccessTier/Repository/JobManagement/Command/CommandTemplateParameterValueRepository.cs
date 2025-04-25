using HEAppE.DataAccessTier.IRepository.JobManagement.Command;
using HEAppE.DomainObjects.JobManagement;

namespace HEAppE.DataAccessTier.Repository.JobManagement.Command;

internal class CommandTemplateParameterValueRepository : GenericRepository<CommandTemplateParameterValue>,
    ICommandTemplateParameterValueRepository
{
    #region Constructors

    internal CommandTemplateParameterValueRepository(MiddlewareContext context)
        : base(context)
    {
    }

    #endregion
}