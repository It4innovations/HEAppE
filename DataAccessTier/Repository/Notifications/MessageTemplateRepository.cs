using HEAppE.DataAccessTier.IRepository.Notifications;
using HEAppE.DomainObjects.Notifications;

namespace HEAppE.DataAccessTier.Repository.Notifications
{
    internal class MessageTemplateRepository : GenericRepository<MessageTemplate>, IMessageTemplateRepository
    {
        #region Constructors
        internal MessageTemplateRepository(MiddlewareContext context)
            : base(context)
        {

        }
        #endregion
    }
}