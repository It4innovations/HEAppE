using System.ComponentModel;
using System.Runtime.Serialization;
using HEAppE.RestApiModels.AbstractModels;

namespace HEAppE.RestApiModels.Management;

/// <summary>
/// Remove sub project model
/// </summary>
[DataContract(Name = "RemoveSubProjectModel")]
[Description("Remove sub project model")]
public class RemoveSubProjectModel : SessionCodeModel
{
    /// <summary>
    /// Id
    /// </summary>
    [DataMember(Name = "Id", IsRequired = true)]
    [Description("Id")]
    public long Id { get; set; }

    public override string ToString()
    {
        return $"DeleteSubProjectModel({base.ToString()}; Id: {Id})";
    }
}