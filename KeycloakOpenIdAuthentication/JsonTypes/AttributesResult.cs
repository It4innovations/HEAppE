using Newtonsoft.Json;
using System.Collections.Generic;

namespace HEAppE.KeycloakOpenIdAuthentication.JsonTypes
{
    public class AttributesResult
    {
        /// <summary>
        /// Collection of projects which can be listed by the user.
        /// </summary>
        [JsonProperty("prj_list")]
        public IEnumerable<AccessRightsResult> ProjectListRights { get; set; }

        /// <summary>
        /// Collection of projects which can be read by the user.
        /// </summary>
        [JsonProperty("prj_read")]
        public IEnumerable<AccessRightsResult> ProjectReadRights { get; set; }

        /// <summary>
        /// Collection of projects which can be written to by the user.
        /// </summary>
        [JsonProperty("prj_write")]
        public IEnumerable<AccessRightsResult> ProjectWriteRights { get; set; }
    }
}
