using System.Runtime.Serialization;
using HEAppE.RestApiModels.AbstractModels;

namespace HEAppE.RestApiModels.Management;

[DataContract(Name = "RemoveSubProjectModel")]
public class RemoveSubProjectModel : SessionCodeModel
{
    [DataMember(Name = "Id", IsRequired = true)]
    public long Id { get; set; }

    public override string ToString()
    {
        return $"DeleteSubProjectModel({base.ToString()}; Id: {Id})";
    }
}