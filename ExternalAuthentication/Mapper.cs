using HEAppE.ExternalAuthentication.Configuration;
using HEAppE.ExternalAuthentication.DTO;
using HEAppE.ExternalAuthentication.DTO.JsonTypes;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace HEAppE.ExternalAuthentication
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
                                 ? $"{ExternalAuthConfiguration.HEAppEUserPrefix}{obj.Email}"
                                 : $"{ExternalAuthConfiguration.HEAppEUserPrefix}{Regex.Replace(obj.PreferredUsername, @"\s+", " ", RegexOptions.Compiled)}",
                Projects = GetProjectWithRoleMapping(obj)
            };
        }

        private static IEnumerable<ProjectOpenId> GetProjectWithRoleMapping(KeycloakUserInfoResult obj)
        {
            try
            {
                var projectRoleMapping = new Dictionary<string, ProjectOpenId>();
                bool hasMappedGroup = false;
                foreach (var propertyInfo in obj.Attributes.GetType().GetProperties())
                {
                    var jsonPropertyName = propertyInfo?.GetCustomAttribute<JsonPropertyAttribute>().PropertyName ?? string.Empty;
                    if (ExternalAuthConfiguration.RoleMapping.TryGetValue(jsonPropertyName, out string mappedRole))
                    {
                        List<string> projectIds = (propertyInfo.GetValue(obj.Attributes) as IEnumerable<AccessRightsResult>)?.Select(s => s.ProjectId).ToList();
                        var projects = ExternalAuthConfiguration.Projects.Where(w => projectIds.Contains(w.UUID));

                        if (projects is null)
                        {
                            continue;
                        }
                        hasMappedGroup = true;
                        foreach (ExternalAuthProjectConfiguration project in projects)
                        {

                            if (projectRoleMapping.TryGetValue(project.UUID, out ProjectOpenId pp))
                            {
                                if (!pp.Roles.Contains(mappedRole))
                                {
                                    pp.Roles.Add(mappedRole);
                                }
                            }
                            else
                            {
                                projectRoleMapping.Add(project.UUID, new ProjectOpenId()
                                {
                                    Name = project.Name,
                                    UUID = project.UUID,
                                    HEAppEGroupName = project.HEAppEGroupName,
                                    Roles = new List<string>() { mappedRole }
                                });
                            }
                        }
                    }

                    if (!hasMappedGroup)
                    {
                        continue;
                    }
                }
                return projectRoleMapping.Values;
            }
            catch (Exception)
            {
                return new List<ProjectOpenId>();
            }
        }
    }
}
