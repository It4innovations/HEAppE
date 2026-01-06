using System.Linq;
using HEAppE.DataAccessTier.IRepository.UserAndLimitationManagement;
using HEAppE.DomainObjects.UserAndLimitationManagement;

namespace HEAppE.DataAccessTier.Repository.UserAndLimitationManagement;

internal class SessionCodeRepository : GenericRepository<SessionCode>, ISessionCodeRepository
{
    #region Constructors

    internal SessionCodeRepository(MiddlewareContext context)
        : base(context)
    {
    }

    #endregion

    #region Methods

    public SessionCode GetByUniqueCode(string uniqueCode)
    {
        return _dbSet.SingleOrDefault(w => w.UniqueCode == uniqueCode);
    }

    public SessionCode GetByUser(AdaptorUser user)
    {
        return _dbSet
            .OfType<SessionCode>()
            .OrderByDescending(w=> w.Id)
            .FirstOrDefault(w => w.User.Id == user.Id);
    }



    #endregion
}