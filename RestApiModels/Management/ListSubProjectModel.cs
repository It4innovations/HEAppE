using System.ComponentModel;
using System.Runtime.Serialization;
using HEAppE.RestApiModels.AbstractModels;

namespace HEAppE.RestApiModels.Management;

/// <summary>
/// List sub project model
/// </summary>
[DataContract(Name = "ListSubProjectModel")]
[Description("List sub project model")]
public class ListSubProjectModel : SessionCodeModel
{
    /// <summary>
    /// Id
    /// </summary>
    [DataMember(Name = "Id", IsRequired = true)]
    [Description("Id")]
    public long Id { get; set; }

    public override string ToString()
    {
        return $"ListSubProjectModel({base.ToString()}; Id: {Id})";
    }
}