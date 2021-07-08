using System;

namespace HEAppE.OpenStackAPI.DTO
{
    /// <summary>
    /// OpenStack application credentials from the keystone.
    /// </summary>
    public class ApplicationCredentialsDTO
    {
        #region Properties
        /// <summary>
        /// Gets or sets the application credentials id.
        /// </summary>
        public string ApplicationCredentialsId { get; set; }

        /// <summary>
        /// Gets or sets the application credentials secret.
        /// </summary>
        public string ApplicationCredentialsSecret { get; set; }

        /// <summary>
        /// Gets or sets the session expiration DateTime.
        /// </summary>
        public DateTime ExpiresAt { get; set; }
        #endregion
    }
}