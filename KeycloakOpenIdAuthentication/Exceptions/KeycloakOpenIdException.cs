using System;
using HEAppE.RestUtils.Interfaces;

namespace HEAppE.KeycloakOpenIdAuthentication.Exceptions
{
    /// <summary>
    /// KeyCloak Exception
    /// </summary>
    public class KeycloakOpenIdException : ExceptionWithMessageAndInnerException
    {
        #region Constructors
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="message">Message</param>
        internal KeycloakOpenIdException(string message) : base(message)
        {

        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="message">Message</param>
        /// <param name="innerException">Inner exception</param>
        internal KeycloakOpenIdException(string message, Exception innerException) : base(message, innerException)
        {

        }
        #endregion
    }
}