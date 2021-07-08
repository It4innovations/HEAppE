using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HEAppE.OpenStackAPI.JsonTypes.Authentication
{
    public class AuthenticationWrapper
    {
        #region Properties
        [JsonProperty("identity")]
        public Identity Identity { get; set; }

        [JsonProperty("scope")]
        public Scope Scope { get; set; }
        #endregion
    }
}
