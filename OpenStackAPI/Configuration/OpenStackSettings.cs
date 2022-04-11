using System;

namespace HEAppE.OpenStackAPI.Configuration
{
    public class OpenStackSettings
    {
        #region Properties
        /// <summary>
        /// Url for Identity API.
        /// </summary>
        public static int IdentityPort { get; set; }

        /// <summary>
        /// Client connection timeout in miliseconds
        /// </summary>
        public static int ConnectionTimeout { get; set; } = 15000;

        /// <summary>
        /// Version of OpenStack.
        /// </summary>
        public static int OpenStackVersion { get; set; }

        /// <summary>
        /// OpenStack session expiration in seconds.
        /// </summary>
        public static int OpenStackSessionExpiration { get; set; }
        #endregion
    }
}