using System;
using System.Text;
using Newtonsoft.Json;

namespace HEAppE.OpenStackAPI.JsonTypes
{
    public class ErrorResponseObject
    {
        #region Properties
        [JsonProperty("error")]
        public ErrorResponse ErrorResponse { get; set; }
        #endregion
        #region Override Methods
        public override string ToString() => ErrorResponse != null ? ErrorResponse.ToString() : base.ToString();
        #endregion
    }
}