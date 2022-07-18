using System;

namespace HEAppE.FileTransferFramework.Exceptions
{
    /// <summary>
    /// SFTP command exception
    /// </summary>
    public class SFTPCommandException : ApplicationException
    {
        #region Constructors
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="message"></param>
        public SFTPCommandException(string message) : base(message) { }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="message"></param>
        /// <param name="internalException"></param>
        public SFTPCommandException(string message, Exception internalException) : base(message, internalException) { }
        #endregion
    }
}
