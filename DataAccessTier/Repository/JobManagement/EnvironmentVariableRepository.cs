using HEAppE.DataAccessTier.IRepository.JobManagement;
using HEAppE.DomainObjects.JobManagement;

namespace HEAppE.DataAccessTier.Repository.JobManagement;

internal class EnvironmentVariableRepository : GenericRepository<EnvironmentVariable>, IEnvironmentVariableRepository
{
    #region Constructors

    internal EnvironmentVariableRepository(MiddlewareContext context)
        : base(context)
    {
    }

    #endregion
}