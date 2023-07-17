using HEAppE.DataAccessTier.IRepository.UserAndLimitationManagement;
using HEAppE.DomainObjects.UserAndLimitationManagement;
using System.Linq;

namespace HEAppE.DataAccessTier.Repository.UserAndLimitationManagement
{
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
            return GetAll().Where(w => w.UniqueCode == uniqueCode)
                         .FirstOrDefault();
        }

        public SessionCode GetByUser(AdaptorUser user)
        {
            return GetAll().Where(w => w.User.Username == user.Username)
                         .FirstOrDefault();
        }
        #endregion
    }
}