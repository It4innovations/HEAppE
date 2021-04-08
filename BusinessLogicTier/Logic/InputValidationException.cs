using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HEAppE.BusinessLogicTier.Logic {
	public class InputValidationException : ExternallyVisibleException {
		public InputValidationException(string message) : base(message) {}
	}
}
