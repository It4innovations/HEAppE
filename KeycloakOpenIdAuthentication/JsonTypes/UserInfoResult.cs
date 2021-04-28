using Newtonsoft.Json;

namespace HEAppE.KeycloakOpenIdAuthentication.JsonTypes
{
    /// <summary>
    /// User info result.
    /// <note>https://connect2id.com/products/server/docs/api/userinfo</note>
    /// <note>https://openid.net/specs/openid-connect-core-1_0.html#StandardClaims</note>
    /// </summary>
    public class UserInfoResult
    {
        /// <summary>
        /// The subject (end-user) identifier.
        /// </summary>
        [JsonProperty("sub", Required = Required.Always)]
        public string Sub { get; set; }

        /// <summary>
        /// The full name of the end-user.
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>
        /// The given or first name of the end-user.
        /// </summary>
        [JsonProperty("given_name")]
        public string GivenName { get; set; }

        /// <summary>
        /// The surname(s) or last name(s) of the end-user.
        /// </summary>
        [JsonProperty("family_name")]
        public string FamilyName { get; set; }

        /// <summary>
        /// The middle name of the end-user.
        /// </summary>
        [JsonProperty("middle_name")]
        public string MiddleName { get; set; }

        /// <summary>
        /// The casual name of the end-user.
        /// </summary>
        [JsonProperty("nickname")]
        public string NickName { get; set; }

        /// <summary>
        /// The username by which the end-user wants to be referred to at the client application.
        /// </summary>
        [JsonProperty("preferred_username")]
        public string PreferredUsername { get; set; }

        /// <summary>
        /// The URL of the profile page for the end-user.
        /// </summary>
        [JsonProperty("profile")]
        public string Profile { get; set; }

        /// <summary>
        /// The URL of the profile picture for the end-user.
        /// </summary>
        [JsonProperty("picture")]
        public string Picture { get; set; }

        /// <summary>
        /// The URL of the end-user's web page or blog.
        /// </summary>
        [JsonProperty("website")]
        public string Website { get; set; }

        /// <summary>
        /// The end-user's preferred email address.
        /// </summary>
        [JsonProperty("email")]
        public string Email { get; set; }

        /// <summary>
        /// True if the end-user's email address has been verified, else false.
        /// </summary>
        [JsonProperty("email_verified")]
        public bool EmailVerified { get; set; }

        /// <summary>
        /// The end-user's gender.
        /// </summary>
        [JsonProperty("gender")]
        public string Gender { get; set; }

        /// <summary>
        /// The end-user's birthday, represented in ISO 8601:2004 YYYY-MM-DD format.
        /// </summary>
        [JsonProperty("birthdate")]
        public string BirthDate { get; set; }

        /// <summary>
        /// The end-user's time zone, e.g. Europe/Paris.
        /// </summary>
        [JsonProperty("zoneinfo")]
        public string ZoneInfo { get; set; }

        /// <summary>
        /// The end-user's locale, represented as a BCP47 language tag. This is typically an ISO 639-1 Alpha-2 language code in lowercase and an ISO 3166-1 Alpha-2 country code in uppercase, separated by a dash. For example, en-US or fr-CA.
        /// </summary>
        [JsonProperty("locale")]
        public string Locale { get; set; }

        /// <summary>
        /// The end-user's preferred telephone number, typically in E.164 format, for example +1 (425) 555-1212.
        /// </summary>
        [JsonProperty("phone_number")]
        public string PhoneNumber { get; set; }

        /// <summary>
        /// True if the end-user's telephone number has been verified, else false.
        /// </summary>
        [JsonProperty("phone_number_verified")]
        public bool PhoneNumberVerified { get; set; }

        /// <summary>
        /// A JSON object describing the end-user's preferred postal address.
        /// </summary>
        [JsonProperty("address")]
        public AddressUserInfoResult Address { get; set; }

        /// <summary>
        /// Time the end-user's information was last updated, as number of seconds since the Unix epoch (1970-01-01T0:0:0Z) as measured in UTC until the date/time.
        /// </summary>
        [JsonProperty("updated_at")]
        public long UpdatedAt { get; set; }
    }
}
