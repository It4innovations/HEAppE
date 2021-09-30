using Newtonsoft.Json;

namespace HEAppE.KeycloakOpenIdAuthentication.JsonTypes
{
    /// <summary>
    /// Address user info result
    /// </summary>
    public class AddressUserInfoResult
    {
        /// <summary>
        /// The full mailing address, with multiple lines if necessary. Newlines can be represented either as a \r\n or as a \n.
        /// </summary>
        [JsonProperty("formatted")]
        public string Formatted { get; set; }

        /// <summary>
        /// The street address component, which may include house number, stree name, post office box, and other multi-line information. Newlines can be represented either as a \r\n or as a \n.
        /// </summary>
        [JsonProperty("street_address")]
        public string StreetAddress { get; set; }

        /// <summary>
        /// City or locality component.
        /// </summary>
        [JsonProperty("locality")]
        public string Locality { get; set; }

        /// <summary>
        /// State, province, prefecture or region component.
        /// </summary>
        [JsonProperty("region")]
        public string Region { get; set; }

        /// <summary>
        /// Zip code or postal code component.
        /// </summary>
        [JsonProperty("postal_code")]
        public string PostalCode { get; set; }

        /// <summary>
        /// Country name component.
        /// </summary>
        [JsonProperty("country")]
        public string Country { get; set; }
    }
}
