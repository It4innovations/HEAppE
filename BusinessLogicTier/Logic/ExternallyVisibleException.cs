using System;

namespace HEAppE.BusinessLogicTier.Logic {
	public class ExternallyVisibleException : ApplicationException {
		public ExternallyVisibleException(string message) : base(message) {}
		public ExternallyVisibleException(string message, Exception innerException) : base(message, innerException) {}
	}
}