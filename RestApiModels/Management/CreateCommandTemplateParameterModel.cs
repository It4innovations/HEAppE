using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using HEAppE.RestApiModels.AbstractModels;

namespace HEAppE.RestApiModels.Management;

/// <summary>
/// Create command template parameter model
/// </summary>
[DataContract(Name = "CreateCommandTemplateParameterModel")]
[Description("Create command template parameter model")]
public class CreateCommandTemplateParameterModel : SessionCodeModel
{
    /// <summary>
    /// Identifier
    /// </summary>
    [DataMember(Name = "Identifier", IsRequired = true)]
    [StringLength(20)]
    [Description("Identifier")]
    public string Identifier { get; set; }

    /// <summary>
    /// Query
    /// </summary>
    [DataMember(Name = "Query", IsRequired = true)]
    [StringLength(200)]
    [Description("Query")]
    public string Query { get; set; }

    /// <summary>
    /// Description
    /// </summary>
    [DataMember(Name = "Description", IsRequired = true)]
    [StringLength(200)]
    [Description("Description")]
    public string Description { get; set; }

    /// <summary>
    /// Command template id
    /// </summary>
    [DataMember(Name = "CommandTemplateId", IsRequired = true)]
    [Description("Command template id")]
    public long CommandTemplateId { get; set; }

    public override string ToString()
    {
        return $"CreateCommandTemplateParameterModel: Identifier={Identifier}, Query={Query}, Description={Description}, CommandTemplateId={CommandTemplateId}";
    }
}