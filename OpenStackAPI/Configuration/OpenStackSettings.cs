using System;

namespace HEAppE.OpenStackAPI.Configuration
{
    public class OpenStackSettings
    {
        #region Properties
        /// <summary>
        /// Port for Identity API
        /// </summary>
        public static int IdentityPort { get; set; }

        /// <summary>
        /// Client connection timeout in seconds
        /// </summary>
        public static double ConnectionTimeout { get; set; } = 15;

        /// <summary>
        /// Version of OpenStack
        /// </summary>
        public static int OpenStackVersion { get; set; }

        /// <summary>
        /// OpenStack session expiration in seconds
        /// </summary>
        public static int OpenStackSessionExpiration { get; set; }
        #endregion
    }
}