using HEAppE.BusinessLogicTier.Logic.DataTransfer.Exceptions;
using HEAppE.BusinessLogicTier.Logic.JobManagement.Exceptions;
using HEAppE.BusinessLogicTier.Logic.JobReporting.Exceptions;
using HEAppE.BusinessLogicTier.Logic.UserAndLimitationManagement.Exceptions;
using log4net;
using System;

namespace HEAppE.BusinessLogicTier.Logic
{
    public static class ExceptionHandler
    {
        private static ILog log = LogManager.GetLogger(typeof(ExceptionHandler));

        public static void ThrowProperExternalException(Exception exception)
        {
            // Throw proper externally visible exceptions
            if (exception is ExternallyVisibleException)
            {
                if(exception is ProjectConfigurationException)
                {
                    throw new ProjectConfigurationException(exception.Message);
                }
                else if(exception is AdaptorUserNotReferencedForProjectException)
                {
                    throw new AdaptorUserNotReferencedForProjectException(exception.Message);
                }
                else if (exception is InputValidationException)
                {
                    throw new InputValidationException(exception.Message);
                }
                else if (exception is RequestedJobResourcesExceededUserLimitationsException)
                {
                    throw new RequestedJobResourcesExceededUserLimitationsException(exception.Message);
                }
                else if (exception is RequestedObjectDoesNotExistException)
                {
                    throw new RequestedObjectDoesNotExistException(exception.Message);
                }
                else if (exception is AdaptorUserNotAuthorizedForJobException)
                {
                    throw new AdaptorUserNotAuthorizedForJobException(exception.Message);
                }
                else if (exception is AuthenticatedUserAlreadyDeletedException)
                {
                    throw new AuthenticatedUserAlreadyDeletedException(exception.Message);
                }
                else if (exception is InvalidAuthenticationCredentialsException)
                {
                    throw new InvalidAuthenticationCredentialsException(exception.Message);
                }
                else if (exception is SessionCodeNotValidException)
                {
                    throw new SessionCodeNotValidException(exception.Message);
                }
                else if (exception is NotAllowedException)
                {
                    throw new NotAllowedException(exception.Message);
                }
                else if (exception is UnableToCreateConnectionException)
                {
                    throw new UnableToCreateConnectionException(exception.Message);
                }
                else if (exception is InvalidRequestException)
                {
                    throw new InvalidRequestException(exception.Message);
                }
                else if (exception is InsufficientRoleException)
                {
                    throw new InsufficientRoleException(exception.Message);
                }
                if (exception is OpenIdAuthenticationException)
                {
                    throw new OpenIdAuthenticationException(exception.Message);
                }
                else
                {
                    log.ErrorFormat("Unknown: {0}: {1},\nInner: {2}", exception.ToString(), exception.Message, (exception.InnerException != null) ? exception.InnerException.Message : "");
                    //throw new InternalMiddlewareException("Unknown internal exception occured. Contact the administrators.");
                    throw new InternalMiddlewareException(exception.Message);
                }
            }
            else
            {
                log.ErrorFormat("Unhandled internal exception: {0}: {1},\nInner: {2}", exception.ToString(), exception.Message, (exception.InnerException != null) ? exception.InnerException.Message : "");
                //throw new InternalMiddlewareException("Internal exception occured. Contact the administrators.");
                throw new InternalMiddlewareException(exception.Message);
            }
        }
    }
}
