﻿using System;

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