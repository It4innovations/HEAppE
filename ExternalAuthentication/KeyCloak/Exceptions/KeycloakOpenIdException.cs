﻿using HEAppE.RestUtils.Interfaces;
using System;

namespace HEAppE.ExternalAuthentication.KeyCloak.Exceptions
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
        public KeycloakOpenIdException(string message) : base(message)
        {

        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="message">Message</param>
        /// <param name="innerException">Inner exception</param>
        public KeycloakOpenIdException(string message, Exception innerException) : base(message, innerException)
        {

        }
        #endregion
    }
}