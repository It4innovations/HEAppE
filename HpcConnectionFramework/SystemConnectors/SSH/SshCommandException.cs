using System;
using System.Collections.Generic;
using System.Text;

namespace HEAppE.HpcConnectionFramework.SystemConnectors.SSH
{
    /// <summary>
    /// Class: Ssh command exception
    /// </summary>
    public class SshCommandException : ApplicationException
    {
        #region Constructors
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="message"></param>
        public SshCommandException(string message) : base(message) { }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="message"></param>
        /// <param name="internalException"></param>
        public SshCommandException(string message, Exception internalException) : base(message, internalException) { }
        #endregion
    }
}
