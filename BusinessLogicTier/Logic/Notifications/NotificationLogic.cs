using HEAppE.DataAccessTier.UnitOfWork;

namespace HEAppE.BusinessLogicTier.Logic.Notifications {
	internal class NotificationLogic : INotificationLogic {
		private readonly IUnitOfWork unitOfWork;

		internal NotificationLogic(IUnitOfWork unitOfWork) {
			this.unitOfWork = unitOfWork;
		}
	}
}