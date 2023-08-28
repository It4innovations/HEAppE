using System;
using System.Collections.Generic;

namespace HEAppE.ExternalAuthentication.DTO;

public record LexisUserRole(Guid UserSid, string UserEmail, string RoleName, string ProjectShortName, List<string> SystemPermissionTypes)
{
  private LexisUserRole() : this(Guid.Empty, string.Empty, string.Empty, string.Empty, new List<string>())
  {

  }
  public static LexisUserRole Empty => new LexisUserRole();

  public static ProjectOpenId ToProjectOpenId(this LexisUserRole role)
  {
    return new ProjectOpenId
    {
      Name = role.ProjectShortName,
      UUID = role,
      HEAppEGroupName
    };
  }
}