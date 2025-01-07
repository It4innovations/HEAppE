using System.Runtime.Serialization;
using HEAppE.RestApiModels.AbstractModels;

namespace HEAppE.RestApiModels.Management;

[DataContract(Name = "ListCommandTemplatesModel")]
public class ListCommandTemplatesModel : SessionCodeModel
{
    [DataMember(Name = "ProjectId", IsRequired = true)]
    public long ProjectId { get; set; }

    public override string ToString()
    {
        return $"ListCommandTemplatesModel({base.ToString()}; ProjectId: {ProjectId})";
    }
}