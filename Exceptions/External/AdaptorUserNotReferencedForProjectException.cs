using HEAppE.Exceptions.AbstractTypes;

namespace HEAppE.Exceptions.External
{
    public class AdaptorUserNotReferencedForProjectException : ExternalException {
		public AdaptorUserNotReferencedForProjectException(string message) : base(message) {}
	}
}