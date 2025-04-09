using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using HEAppE.RestApiModels.AbstractModels;

namespace HEAppE.RestApiModels.JobManagement;

/// <summary>
/// Model to copy job data from temp
/// </summary>
[DataContract(Name = "CopyJobDataFromTempModel")]
[Description("Model to copy job data from temp")]
public class CopyJobDataFromTempModel : CreatedJobInfoModel
{
    /// <summary>
    /// Temp session code
    /// </summary>
    [DataMember(Name = "TempSessionCode")]
    [StringLength(50)]
    [Description("Temp session code")]
    public string TempSessionCode { get; set; }

    public override string ToString()
    {
        return $"CopyJobDataFromTempModel({base.ToString()}; TempSessionCode: {TempSessionCode})";
    }
}