using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace HEAppE.RestApiModels.Management;

/// <summary>
/// Create command inner template parameter model
/// </summary>
[DataContract(Name = "CreateCommandInnerTemplateParameterModel")]
[Description("Create command inner template parameter model")]
public class CreateCommandInnerTemplateParameterModel
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
    [DataMember(Name = "Query", IsRequired = false)]
    [StringLength(200)]
    [Description("Query")]
    public string Query { get; set; }

    /// <summary>
    /// Description
    /// </summary>
    [DataMember(Name = "Description", IsRequired = false)]
    [StringLength(200)]
    [Description("Description")]
    public string Description { get; set; }

    public override string ToString()
    {
        return $"CreateCommandInnerTemplateParameterModel: Identifier={Identifier}, Query={Query}, Description={Description}";
    }
}