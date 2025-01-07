using System.Runtime.Serialization;
using HEAppE.ExtModels.JobManagement.Models;

namespace HEAppE.ExtModels.UserAndLimitationManagement.Models;

[DataContract(Name = "AdaptorUserGroupExt")]
public class AdaptorUserGroupExt
{
    [DataMember(Name = "Id")] public long? Id { get; set; }

    [DataMember(Name = "Name")] public string Name { get; set; }

    [DataMember(Name = "Description")] public string Description { get; set; }

    [DataMember(Name = "Project")] public ProjectExt Project { get; set; }

    [DataMember(Name = "Roles")] public string[] Roles { get; set; }

    public override string ToString()
    {
        return $"AdaptorUserGroupExt(id={Id}; name={Name}; description={Description}; Project={Project})";
    }
}