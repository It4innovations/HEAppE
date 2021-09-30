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
        /// Name of the OpenStack role, which is required to access OpenStack.
        /// </summary>
        public static string OpenStackRoleName { get; set; }
    }
}