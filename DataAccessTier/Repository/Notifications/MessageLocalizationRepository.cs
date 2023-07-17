using HEAppE.DataAccessTier.IRepository.Notifications;
using HEAppE.DomainObjects.Notifications;

namespace HEAppE.DataAccessTier.Repository.Notifications
{
    internal class MessageLocalizationRepository : GenericRepository<MessageLocalization>, IMessageLocalizationRepository
    {
        #region Constructors
        internal MessageLocalizationRepository(MiddlewareContext context)
            : base(context)
        {

        }
        #endregion
    }
}
