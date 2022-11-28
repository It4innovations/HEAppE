using Newtonsoft.Json;
using System.Collections.Generic;
using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;

namespace HEAppE.ExternalAuthentication.JsonTypes
{
    /// <summary>
    /// Wrapper around decoded JwtSecurityToken, which simplifies access to required fields and claims.
    /// </summary>
    public class DecodedAccessToken
    {
        #region PrivateDetails
        private class RawJwtAttributes
        {
            /// <summary>
            /// Collection of projects which can be listed by the user.
            /// </summary>
            [JsonProperty("prj_list")]
            public List<string> ProjectListRights { get; set; }

            /// <summary>
            /// Collection of projects which can be read by the user.
            /// </summary>
            [JsonProperty("prj_read")]
            public List<string> ProjectReadRights { get; set; }

            /// <summary>
            /// Collection of projects which can be written to by the user.
            /// </summary>
            [JsonProperty("prj_write")]
            public List<string> ProjectWriteRights { get; set; }
        }
        private JwtSecurityToken _token;
        private ParsedJwtAttributes _parsedAttributes = null;
        #endregion


        /// <summary>
        /// Create Decoded access token wrapper from decoded JWT token.
        /// </summary>
        /// <param name="decodedToken">Decoded JWT token.</param>
        internal DecodedAccessToken(JwtSecurityToken decodedToken)
        {
            _token = decodedToken;
        }

        #region Methods


        /// <summary>
        /// Get claim value by its type/name.
        /// </summary>
        /// <param name="claimName">Claim name/type.</param>
        /// <returns>Claim value or null if the claim is not available.</returns>
        private string GetClaim(string claimName) => _token.Claims.Single(claim => claim.Type == claimName)?.Value;

        
        /// <summary>
        /// Parse organization-project pairds.
        /// </summary>
        /// <param name="rawPairs">Raw collection of pairs.</param>
        /// <param name="parsedPairs">Collection of parsed pairs.</param>
        private void ParsePairs(IEnumerable<string> rawPairs, List<ParsedJwtAttributes.OrganizationProjectPair> parsedPairs)
        {
            bool ParseOrganizationProjectPair(string rawPair, out ParsedJwtAttributes.OrganizationProjectPair parsedPair)
            {
                parsedPair = null;
                rawPair = rawPair.ToUpper();
                //"ORG=IT4I,PRJ=TEST1"
                if (!rawPair.StartsWith("ORG="))
                {
                    return false;
                }
                var delimiterIndex = rawPair.IndexOf(',');
                if (delimiterIndex == -1 || delimiterIndex == rawPair.Length - 1)
                {
                    return false;
                }

                // NOTE: from 4 to skip 'ORG='
                var organization = rawPair[4..delimiterIndex];
                // NOTE: from 5 to skip organization + 'ORG=' + delimiter
                var project = rawPair[(delimiterIndex + 5)..];

                parsedPair = new ParsedJwtAttributes.OrganizationProjectPair(organization, project);
                return true;
            }


            foreach (var rawPair in rawPairs)
            {
                if (ParseOrganizationProjectPair(rawPair, out var parsed))
                {
                    parsedPairs.Add(parsed);
                }
            }
        }

        /// <summary>
        /// Load access attributes from the attributes claim.
        /// </summary>
        private void LoadAccessAttributes()
        {
            // Check if already parsed.
            if (_parsedAttributes != null)
            {
                return;
            }
            _parsedAttributes = new ParsedJwtAttributes();
            var attributes = GetClaim("attributes");
            if (string.IsNullOrWhiteSpace(attributes))
            {
                return;
            }

            RawJwtAttributes deserializedAttributes;
            try
            {
                deserializedAttributes = JsonConvert.DeserializeObject<RawJwtAttributes>(attributes);
            }
            catch (System.Exception)
            {
                Debug.Assert(false);
                return;
            }

            ParsePairs(deserializedAttributes.ProjectListRights, _parsedAttributes.ListPairs);
            ParsePairs(deserializedAttributes.ProjectReadRights, _parsedAttributes.ReadPairs);
            ParsePairs(deserializedAttributes.ProjectWriteRights, _parsedAttributes.WritePairs);
        }

       
        /// <summary>
        /// Claim value indexer.
        /// </summary>
        /// <param name="claimName">Claim name.</param>
        /// <returns>Claim value or null if the claim is not available.</returns>
        public string this[string claimName] => GetClaim(claimName);

        /// <summary>
        /// Get flag if user email is verified.
        /// </summary>
        public bool IsEmailVerified => bool.Parse(GetClaim("email_verified"));

        /// <summary>
        /// User email.
        /// </summary>
        public string Email => GetClaim("email");

        /// <summary>
        /// User name.
        /// </summary>
        public string Username => GetClaim("name");

        /// <summary>
        /// Prefered username.
        /// </summary>
        public string PreferedUsername => GetClaim("preferred_username");

        /// <summary>
        /// Authorized party - the party to which the ID Token was issued.
        /// </summary>
        public string AuthorizedParty => GetClaim("azp");


        /// <summary>
        /// Check if user has list right to project.
        /// </summary>
        /// <param name="project">Project name.</param>
        /// <param name="organization">Organization name.</param>
        /// <returns>True if user can list project.</returns>
        public bool CanListProject(string project, string organization)
        {
            LoadAccessAttributes();
            Debug.Assert(_parsedAttributes is not null);
            return _parsedAttributes.HasAccess(ParsedJwtAttributes.AccessType.List, organization, project);
        }

        /// <summary>
        /// Check if user has read right to project.
        /// </summary>
        /// <param name="project">Project name.</param>
        /// <param name="organization">Organization name.</param>
        /// <returns>True if user can read project.</returns>
        public bool CanReadProject(string project, string organization)
        {
            LoadAccessAttributes();
            Debug.Assert(_parsedAttributes is not null);
            return _parsedAttributes.HasAccess(ParsedJwtAttributes.AccessType.Read, organization, project);
        }

        /// <summary>
        /// Check that the base claims (iss, sub, azp, email, preferred_username) of both tokens match.
        /// </summary>
        /// <param name="refreshed">Refreshed access token.</param>
        /// <returns>True if base claims matches.</returns>
        internal bool BaseClaimsMatch(DecodedAccessToken refreshed)
        {
            var refreshedToken = refreshed._token;
            if (_token.Issuer != refreshedToken.Issuer)
                return false;
            if (_token.Subject != refreshedToken.Subject)
                return false;
            if (AuthorizedParty != refreshed.AuthorizedParty)
                return false;
            if (PreferedUsername != refreshed.PreferedUsername || Email != refreshed.Email)
                return false;

            return true;
        }

        /// <summary>
        /// Check if user has write right to project.
        /// </summary>
        /// <param name="project">Project name.</param>
        /// <param name="organization">Organization name.</param>
        /// <returns>True if user can write project.</returns>
        public bool CanWriteProject(string project, string organization)
        {
            LoadAccessAttributes();
            Debug.Assert(_parsedAttributes is not null);
            return _parsedAttributes.HasAccess(ParsedJwtAttributes.AccessType.Write, organization, project);
        }

        /// <summary>
        /// Check if user has list right to project. Default keycloak organization is used.
        /// </summary>
        /// <param name="project">Project name.</param>
        /// <returns>True if user can list project.</returns>
        public bool CanListProject(string project) => CanListProject(project: project, organization: KeycloakSettings.Organization);

        /// <summary>
        /// Check if user has read right to project. Default keycloak organization is used.
        /// </summary>
        /// <param name="project">Project name.</param>
        /// <returns>True if user can read project.</returns>
        public bool CanReadProject(string project) => CanReadProject(project: project, organization: KeycloakSettings.Organization);

        /// <summary>
        /// Check if user has write right to project. Default keycloak organization is used.
        /// </summary>
        /// <param name="project">Project name.</param>
        /// <returns>True if user can write project.</returns>
        public bool CanWriteProject(string project) => CanWriteProject(project: project, organization: KeycloakSettings.Organization);
        #endregion
    }
}
