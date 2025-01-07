using System.Runtime.Serialization;
using HEAppE.RestApiModels.AbstractModels;

namespace HEAppE.RestApiModels.Management;

[DataContract(Name = "ListSubProjectModel")]
public class ListSubProjectModel : SessionCodeModel
{
    [DataMember(Name = "Id", IsRequired = true)]
    public long Id { get; set; }

    public override string ToString()
    {
        return $"ListSubProjectModel({base.ToString()}; Id: {Id})";
    }
}