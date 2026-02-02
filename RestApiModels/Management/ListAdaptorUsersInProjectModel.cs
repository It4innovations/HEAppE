using System.ComponentModel;
using System.Runtime.Serialization;
using HEAppE.RestApiModels.AbstractModels;

namespace HEAppE.RestApiModels.Management;

/// <summary>
/// List adaptor users in project model
/// </summary>
public class ListAdaptorUsersInProjectModel : SessionCodeModel
{
    /// <summary>
    /// Project Id
    /// </summary>
    [DataMember(Name = "ProjectId", IsRequired = true)]
    [Description("Project Id")]
    public long ProjectId { get; set;  }
}