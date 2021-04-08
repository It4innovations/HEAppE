using HEAppE.DataAccessTier.IRepository.Notifications;
using HEAppE.DomainObjects.Notifications;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
