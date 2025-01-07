using System.Collections.Generic;

namespace HEAppE.ExternalAuthentication.DTO;

public class UserOpenId
{
    public string UserName { get; set; }
    public string Email { get; set; }
    public string Name { get; set; }
    public string FamilyName { get; set; }
    public string GivenName { get; set; }
    public IEnumerable<ProjectOpenId> Projects { get; set; } = new List<ProjectOpenId>();
}