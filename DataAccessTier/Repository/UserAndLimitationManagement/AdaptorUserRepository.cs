using HEAppE.DataAccessTier.IRepository.UserAndLimitationManagement;
using HEAppE.DomainObjects.UserAndLimitationManagement;
using System.Linq;

namespace HEAppE.DataAccessTier.Repository.UserAndLimitationManagement
{
    internal class AdaptorUserRepository : GenericRepository<AdaptorUser>, IAdaptorUserRepository
    {
        #region Constructors
        internal AdaptorUserRepository(MiddlewareContext context)
            : base(context)
        {

        }
        #endregion
        #region Methods
        public AdaptorUser GetByName(string username)
        {
            return GetAll().Where(w => w.Username == username)
                            .FirstOrDefault();
        }
        #endregion
    }
}