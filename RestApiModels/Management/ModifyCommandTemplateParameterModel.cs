using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using HEAppE.RestApiModels.AbstractModels;

namespace HEAppE.RestApiModels.Management;

[DataContract(Name = "ModifyCommandTemplateParameterModel")]
public class ModifyCommandTemplateParameterModel : SessionCodeModel
{
    [DataMember(Name = "Id", IsRequired = true)]
    public long Id { get; set; }

    [DataMember(Name = "Identifier", IsRequired = true)]
    [StringLength(20)]
    public string Identifier { get; set; }

    [DataMember(Name = "Query", IsRequired = true)]
    [StringLength(200)]
    public string Query { get; set; }

    [DataMember(Name = "Description", IsRequired = true)]
    [StringLength(200)]
    public string Description { get; set; }

    public override string ToString()
    {
        return
            $"ModifyCommandTemplateParameterModel: Id: {Id}, Identifier={Identifier}, Query={Query}, Description={Description}";
    }
}