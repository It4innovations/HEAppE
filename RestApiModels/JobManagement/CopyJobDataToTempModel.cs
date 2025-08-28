using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using HEAppE.RestApiModels.AbstractModels;

namespace HEAppE.RestApiModels.JobManagement;

/// <summary>
/// Model to copy job data to temp
/// </summary>
[DataContract(Name = "CopyJobDataToTempModel")]
[Description("Model to copy job data to temp")]
public class CopyJobDataToTempModel : CreatedJobInfoModel
{
    /// <summary>
    /// Path
    /// </summary>
    [DataMember(Name = "Path")]
    [StringLength(50)]
    [Description("Path")]
    public string Path { get; set; }

    public override string ToString()
    {
        return $"CopyJobDataToTempModel({base.ToString()}; Path: {Path})";
    }
}