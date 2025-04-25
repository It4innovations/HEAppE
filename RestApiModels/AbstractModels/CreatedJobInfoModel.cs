using System.ComponentModel;
using System.Runtime.Serialization;

namespace HEAppE.RestApiModels.AbstractModels;

/// <summary>
/// Create job info model
/// </summary>
[Description("Create job info model")]
public abstract class CreatedJobInfoModel : SessionCodeModel
{
    /// <summary>
    /// Created job info id
    /// </summary>
    [DataMember(Name = "CreatedJobInfoId")]
    [Description("Created job info id")]
    public long CreatedJobInfoId { get; set; }

    public override string ToString()
    {
        return $"GetCommandTemplateParametersNameModel({base.ToString()}; CreatedJobInfoId: {CreatedJobInfoId})";
    }
}