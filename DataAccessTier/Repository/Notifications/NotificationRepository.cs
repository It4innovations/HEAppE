using HEAppE.DataAccessTier.IRepository.Notifications;
using HEAppE.DomainObjects.Notifications;

namespace HEAppE.DataAccessTier.Repository.Notifications
{
    internal class NotificationRepository : GenericRepository<Notification>, INotificationRepository
    {
        #region Constructors
        internal NotificationRepository(MiddlewareContext context)
            : base(context)
        {

        }
        #endregion
    }
}