using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using HEAppE.RestApiModels.AbstractModels;

namespace HEAppE.RestApiModels.Management;

[DataContract(Name = "CreateCommandTemplateParameterModel")]
public class CreateCommandTemplateParameterModel : SessionCodeModel
{
    [DataMember(Name = "Identifier", IsRequired = true)]
    [StringLength(20)]
    public string Identifier { get; set; }

    [DataMember(Name = "Query", IsRequired = true)]
    [StringLength(200)]
    public string Query { get; set; }

    [DataMember(Name = "Description", IsRequired = true)]
    [StringLength(200)]
    public string Description { get; set; }

    [DataMember(Name = "CommandTemplateId", IsRequired = true)]
    public long CommandTemplateId { get; set; }

    public override string ToString()
    {
        return
            $"CreateCommandTemplateParameterModel: Identifier={Identifier}, Query={Query}, Description={Description}, CommandTemplateId={CommandTemplateId}";
    }
}