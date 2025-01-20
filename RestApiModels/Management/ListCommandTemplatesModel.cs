using System.ComponentModel;
using System.Runtime.Serialization;
using HEAppE.RestApiModels.AbstractModels;

namespace HEAppE.RestApiModels.Management;

/// <summary>
/// List command templates model
/// </summary>
[DataContract(Name = "ListCommandTemplatesModel")]
[Description("List command templates model")]
public class ListCommandTemplatesModel : SessionCodeModel
{
    /// <summary>
    /// Project id
    /// </summary>
    [DataMember(Name = "ProjectId", IsRequired = true)]
    [Description("Project id")]
    public long ProjectId { get; set; }

    public override string ToString()
    {
        return $"ListCommandTemplatesModel({base.ToString()}; ProjectId: {ProjectId})";
    }
}