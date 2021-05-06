using HEAppE.KeycloakOpenIdAuthentication.Configuration;
using HEAppE.KeycloakOpenIdAuthentication.JsonTypes;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace HEAppE.KeycloakOpenIdAuthentication
{
    public static class Mapper
    {
        public static UserOpenId Convert(this KeycloakUserInfoResult obj)
        {
            return new UserOpenId()
            {
                Email = obj.Email,
                GivenName = obj.GivenName,
                Name = obj.Name,
                FamilyName = obj.FamilyName,
                UserName = obj.EmailVerified && !string.IsNullOrWhiteSpace(obj.Email)
                                 ? $"{KeycloakConfiguration.HEAppEUserPrefix}{obj.Email}"
                                 : $"{KeycloakConfiguration.HEAppEUserPrefix}{Regex.Replace(obj.PreferredUsername, @"\s+", " ", RegexOptions.Compiled)}",
                ProjectRoles = GetProjectRoleMapping(obj) ?? new Dictionary<string, IEnumerable<string>>()
            };
        }

        private static Dictionary<string, IEnumerable<string>> GetProjectRoleMapping(KeycloakUserInfoResult obj)
        {
            try
            {
                var projectRoleMapping = new Dictionary<string, IEnumerable<string>>();
                foreach (var propertyInfo in obj.Attributes.GetType().GetProperties())
                {
                    var jsonPropertyName = propertyInfo?.GetCustomAttribute<JsonPropertyAttribute>().PropertyName ?? string.Empty;
                    if (KeycloakConfiguration.RoleMapping.TryGetValue(jsonPropertyName, out string mappedRole))
                    {
                        var projectIds = (propertyInfo.GetValue(obj.Attributes) as IEnumerable<AccessRightsResult>)
                                                                            ?.Where(w => w.OrganizationName == KeycloakConfiguration.Organization)
                                                                             .Select(s => s.ProjectName)
                                                                             .ToList();

                        foreach (string projectId in projectIds ?? throw new Exception($"Open-Id: For defined Organization \"{KeycloakConfiguration.Organization}\" are not defined projects!"))
                        {
                            if (projectRoleMapping.TryGetValue(projectId, out IEnumerable<string> availableRoles))
                            {
                                projectRoleMapping[projectId] = availableRoles.Union(new List<string>() { mappedRole });
                            }
                            else
                            {
                                projectRoleMapping.Add(projectId, new List<string>() { mappedRole });
                            }
                        }
                    }
                    else
                    {
                        throw new Exception("Open-Id: Project-role mapping is not correctly defined!");
                    }
                }
                return projectRoleMapping;
            }
            catch (Exception e)
            {
                //TODO Log
                return default;
            }
        }
    }
}
