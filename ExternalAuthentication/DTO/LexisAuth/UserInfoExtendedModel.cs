using System;
using System.Collections.Generic;

namespace HEAppE.ExternalAuthentication.DTO.LexisAuth;

public sealed record UserInfoExtendedModel()
{
  public Guid Id { get; set; }
  public string KeycloakSid { get; set; }
  public string Name { get; set; }
  public string UserName { get; set; }
  public string Email { get; set; }
  public List<UserSystemRoleExtendedModel> SystemRoles { get; set; } = new();
}

public record UserSystemRoleExtendedModel(string ProjectShortName, string UserEmail, string RoleName, IEnumerable<ProjectResourceDetailModel> ProjectResources, List<string> SystemPermissionTypes);

public record ProjectResourceDetailModel(string Name, string Description, DateTime? StartDate, DateTime? EndDate, IList<LocationDetailModel> Locations);
public record LocationDetailModel(string Name, string Descritpion, string Endpoint, bool IsPrivate, string Type, long? AllocationUnitCount, string AllocationUnitType, IEnumerable<KeyValuePair<string, string>> Values);