using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using FluentValidation;
using HEAppE.RestApiModels.AbstractModels;

namespace HEAppE.RestApiModels.FileTransfer;

/// <summary>
/// Model for downloading file from cluster
/// </summary>
[DataContract(Name = "DownloadFileFromClusterModel")]
[Description("Model for downloading file from cluster")]
public class DownloadFileFromClusterModel : SubmittedJobInfoModel
{
    /// <summary>
    /// Relative file path on cluster
    /// </summary>
    [DataMember(Name = "RelativeFilePath")]
    [StringLength(255)]
    [Description("Relative file path on cluster")]
    public string RelativeFilePath { get; set; }

    public override string ToString()
    {
        return $"DownloadFileFromClusterModel({base.ToString()}; RelativeFilePath: {RelativeFilePath})";
    }
}

public class DownloadFileFromClusterModelValidator : AbstractValidator<DownloadFileFromClusterModel>
{
    public DownloadFileFromClusterModelValidator()
    {
        Include(new SubmittedJobInfoModelValidator());
    }
}