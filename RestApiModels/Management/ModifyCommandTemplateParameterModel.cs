using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using HEAppE.RestApiModels.AbstractModels;

namespace HEAppE.RestApiModels.Management;

/// <summary>
/// Modify command template parameter model
/// </summary>
[DataContract(Name = "ModifyCommandTemplateParameterModel")]
[Description("Modify command template parameter model")]
public class ModifyCommandTemplateParameterModel : SessionCodeModel
{
    /// <summary>
    /// Id
    /// </summary>
    [DataMember(Name = "Id", IsRequired = true)]
    [Description("Id")]
    public long Id { get; set; }

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

    public override string ToString()
    {
        return $"ModifyCommandTemplateParameterModel: Id: {Id}, Identifier={Identifier}, Query={Query}, Description={Description}";
    }
}