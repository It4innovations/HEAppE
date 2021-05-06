using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace HEAppE.KeycloakOpenIdAuthentication.JsonTypes
{
    public class AttributesResult
    {
        /// <summary>
        /// Collection of projects which can be listed by the user.
        /// </summary>
        [JsonProperty("prj_list")]
        public IEnumerable<AccessRightsResult> ProjectListRights { get; set; }// = new List<AccessRightsResult>() { new AccessRightsResult() { OrganizationName = "IT4I", ProjectName = "wp5" }};

        /// <summary>
        /// Collection of projects which can be read by the user.
        /// </summary>
        [JsonProperty("prj_read")]
        public IEnumerable<AccessRightsResult> ProjectReadRights { get; set; }// = new List<AccessRightsResult>() { new AccessRightsResult() { OrganizationName = "IT4I", ProjectName = "wp5" }, new AccessRightsResult() { OrganizationName = "IT4I", ProjectName = "wp6" } };

        /// <summary>
        /// Collection of projects which can be written to by the user.
        /// </summary>
        [JsonProperty("prj_write")]
        public IEnumerable<AccessRightsResult> ProjectWriteRights { get; set; }// = new List<AccessRightsResult>() { new AccessRightsResult() { OrganizationName = "IT4I", ProjectName = "wp5" }, new AccessRightsResult() { OrganizationName = "IT4I", ProjectName = "wp6" } };
    }
}
