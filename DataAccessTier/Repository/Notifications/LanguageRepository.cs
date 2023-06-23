using HEAppE.DataAccessTier.IRepository.Notifications;
using HEAppE.DomainObjects.Notifications;

namespace HEAppE.DataAccessTier.Repository.Notifications
{
    internal class LanguageRepository : GenericRepository<Language>, ILanguageRepository
    {
        #region Constructors
        internal LanguageRepository(MiddlewareContext context)
                : base(context)
        {

        }
        #endregion
    }
}
