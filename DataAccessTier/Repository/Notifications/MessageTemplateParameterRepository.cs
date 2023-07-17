using HEAppE.DataAccessTier.IRepository.Notifications;
using HEAppE.DomainObjects.Notifications;

namespace HEAppE.DataAccessTier.Repository.Notifications
{
    internal class MessageTemplateParameterRepository : GenericRepository<MessageTemplateParameter>, IMessageTemplateParameterRepository
    {
        #region Constructors
        internal MessageTemplateParameterRepository(MiddlewareContext context)
            : base(context)
        {

        }
        #endregion
    }
}
