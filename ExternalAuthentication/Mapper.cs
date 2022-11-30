using HEAppE.ExternalAuthentication.Configuration;
using HEAppE.ExternalAuthentication.JsonTypes;
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
                Projects = GetProjectWithRoleMapping(obj) ?? new List<ProjectOpenId>()
            };
        }

        private static IEnumerable<ProjectOpenId> GetProjectWithRoleMapping(KeycloakUserInfoResult obj)
        {
            try
            {
                var projectRoleMapping = new Dictionary<string, ProjectOpenId>();
                foreach (var propertyInfo in obj.Attributes.GetType().GetProperties())
                {
                    var jsonPropertyName = propertyInfo?.GetCustomAttribute<JsonPropertyAttribute>().PropertyName ?? string.Empty;
                    if (ExternalAuthConfiguration.RoleMapping.TryGetValue(jsonPropertyName, out string mappedRole))
                    {
                        var projectIds = (propertyInfo.GetValue(obj.Attributes) as IEnumerable<AccessRightsResult>)
                                                                            ?.Where(w => ExternalAuthConfiguration.Projects.Select(s => s.UUID).Contains(w.ProjectId))
                                                                             .Select(s => s.ProjectName)
                                                                             .ToList();

                        foreach (string projectId in projectIds ?? throw new Exception($"Open-Id: For defined Project Id \"{string.Join(",", ExternalAuthConfiguration.Projects.Select(s=>s.Name))}\" are nothing!"))
                        {
                            if (projectRoleMapping.TryGetValue(projectId, out ProjectOpenId project))
                            {
                                project.Roles.Union(new List<string>() { mappedRole });
                            }
                            else
                            {
                                projectRoleMapping.Add(projectId, new ProjectOpenId()
                                {
                                    Name = projectId,
                                    UUID = projectId,
                                    HEAppEGroupName = "",
                                    Roles = new List<string>() { mappedRole }
                                });
                            }
                        }
                    }
                    else
                    {
                        throw new Exception("Open-Id: Project-role mapping is not correctly defined!");
                    }
                }
                return projectRoleMapping.Values;
            }
            catch (Exception)
            {
                //TODO Log
                return default;
            }
        }
    }
}
