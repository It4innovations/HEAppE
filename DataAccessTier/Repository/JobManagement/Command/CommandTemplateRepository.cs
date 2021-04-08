using HEAppE.DataAccessTier.IRepository.JobManagement.Command;
using HEAppE.DomainObjects.JobManagement;

namespace HEAppE.DataAccessTier.Repository.JobManagement.Command
{
    internal class CommandTemplateRepository : GenericRepository<CommandTemplate>, ICommandTemplateRepository
    {
        #region Constructors
        internal CommandTemplateRepository(MiddlewareContext context)
            : base(context)
        {

        }
        #endregion
    }
}