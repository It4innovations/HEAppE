using System.ComponentModel;
using System.Runtime.Serialization;
using HEAppE.RestApiModels.AbstractModels;

namespace HEAppE.RestApiModels.Management;

/// <summary>
/// List sub projects model
/// </summary>
[DataContract(Name = "ListSubProjectsModel")]
[Description("List sub projects model")]
public class ListSubProjectsModel : SessionCodeModel
{
    /// <summary>
    /// Id
    /// </summary>
    [DataMember(Name = "Id", IsRequired = true)]
    [Description("Id")]
    public long Id { get; set; }

    public override string ToString()
    {
        return $"ListSubProjectsModel({base.ToString()}; Id: {Id})";
    }
}