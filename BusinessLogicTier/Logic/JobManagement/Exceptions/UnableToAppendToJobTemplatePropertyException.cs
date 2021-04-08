using System;

namespace HEAppE.BusinessLogicTier.Logic.JobManagement.Exceptions {
	public class UnableToAppendToJobTemplatePropertyException : ApplicationException {
		public UnableToAppendToJobTemplatePropertyException(string message) : base(message) {}
	}
}