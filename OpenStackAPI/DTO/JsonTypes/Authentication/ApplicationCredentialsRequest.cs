using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace HEAppE.OpenStackAPI.DTO.JsonTypes.Authentication;

public class ApplicationCredentialsRequest
{
    #region Properties

    [JsonProperty("application_credential")]
    public ApplicationCredentials ApplicationCredentials { get; set; }

    #endregion

    #region Methods

    public static ApplicationCredentialsRequest CreateApplicationCredentialsRequest(string uniqueName,
        DateTime expiration, bool restricted = true, IEnumerable<Role> roles = null,
        IEnumerable<AccessRule> accessRules = null)
    {
        var request = new ApplicationCredentialsRequest
        {
            ApplicationCredentials = new ApplicationCredentials
            {
                Name = uniqueName,
                ExpiresAt = expiration,
                Unrestricted = !restricted,
                Description = "Application credentials created by OpenStackAPI library of HEAppE."
            }
        };

        if (roles is not null) request.ApplicationCredentials.Roles = roles.ToList();

        if (accessRules is not null) request.ApplicationCredentials.AccessRules = accessRules.ToList();

        return request;
    }

    #endregion
}