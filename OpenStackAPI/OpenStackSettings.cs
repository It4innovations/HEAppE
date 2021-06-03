using System;

namespace HEAppE.OpenStackAPI
{
    public class OpenStackSettings
    {
        /// <summary>
        /// Url for Identity API.
        /// </summary>
        public static int IdentityPort { get; set; }

        /// <summary>
        /// Version of OpenStack.
        /// </summary>
        public static int OpenStackVersion { get; set; }

        /// <summary>
        /// OpenStack session expiration in seconds.
        /// </summary>
        public static int OpenStackSessionExpiration { get; set; }
    }
}