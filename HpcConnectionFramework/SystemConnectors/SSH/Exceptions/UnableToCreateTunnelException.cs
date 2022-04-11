using System;

namespace HEAppE.HpcConnectionFramework.SystemConnectors.SSH.Exceptions
{
    /// <summary>
    /// Tunnel exception
    /// </summary>
    public class UnableToCreateTunnelException : Exception
    {
        #region Constructors
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="message">Message</param>
        public UnableToCreateTunnelException(string message) : base(message) { }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="message">Message</param>
        /// <param name="internalException">Exception</param>
        public UnableToCreateTunnelException(string message, Exception internalException) : base(message, internalException) { }
        #endregion
    }
}
