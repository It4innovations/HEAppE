using System.Runtime.Serialization;
using HEAppE.RestApiModels.AbstractModels;

namespace HEAppE.RestApiModels.Management;

[DataContract(Name = "RemoveCommandTemplateModel")]
public class RemoveCommandTemplateModel : SessionCodeModel
{
    [DataMember(Name = "Id", IsRequired = true)]
    public long CommandTemplateId { get; set; }

    public override string ToString()
    {
        return $"RemoveCommandTemplateModel({base.ToString()}; CommandTemplateId: {CommandTemplateId})";
    }
}