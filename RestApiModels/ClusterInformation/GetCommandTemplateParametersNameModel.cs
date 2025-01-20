using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using HEAppE.RestApiModels.AbstractModels;

namespace HEAppE.RestApiModels.ClusterInformation;

/// <summary>
/// Model for retrieving command template parameter names
/// </summary>
[DataContract(Name = "GetCommandTemplateParametersNameModele")]
[Description("Model for retrieving command template parameter names")]
public class GetCommandTemplateParametersNameModel : SessionCodeModel
{
    /// <summary>
    /// Command template id
    /// </summary>
    [DataMember(Name = "CommandTemplateId")]
    [Description("Command template id")]
    public long CommandTemplateId { get; set; }

    /// <summary>
    /// Project id
    /// </summary>
    [DataMember(Name = "ProjectId", IsRequired = true)]
    [Description("Project id")]
    public long ProjectId { get; set; }

    /// <summary>
    /// User script path
    /// </summary>
    [DataMember(Name = "UserScriptPath", IsRequired = false)]
    [Description("User script path")]
    [StringLength(250)]
    public string UserScriptPath { get; set; }

    public override string ToString()
    {
        return
            $"GetCommandTemplateParametersNameModel({base.ToString()}; CommandTemplateId: {CommandTemplateId}; UserScriptPath: {UserScriptPath})";
    }
}