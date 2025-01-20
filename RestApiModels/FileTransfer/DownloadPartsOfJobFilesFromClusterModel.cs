using System.ComponentModel;
using System.Linq;
using System.Runtime.Serialization;
using FluentValidation;
using HEAppE.ExtModels.FileTransfer.Models;
using HEAppE.RestApiModels.AbstractModels;

namespace HEAppE.RestApiModels.FileTransfer;

/// <summary>
/// Model for downloading parts of job files from cluster
/// </summary>
[DataContract(Name = "DownloadPartsOfJobFilesFromClusterModel")]
[Description("Model for downloading parts of job files from cluster")]
public class DownloadPartsOfJobFilesFromClusterModel : SubmittedJobInfoModel
{
    /// <summary>
    /// Task file models array
    /// </summary>
    [DataMember(Name = "TaskFileOffsets")]
    [Description("Task file models array")]
    public TaskFileOffsetExt[] TaskFileOffsets { get; set; }

    public override string ToString()
    {
        return
            $"DownloadPartsOfJobFilesFromClusterModel({base.ToString()}; TaskFileOffsets: {string.Join("; ", TaskFileOffsets.ToList())})";
    }
}

public class
    DownloadPartsOfJobFilesFromClusterModelValidator : AbstractValidator<DownloadPartsOfJobFilesFromClusterModel>
{
    public DownloadPartsOfJobFilesFromClusterModelValidator()
    {
        Include(new SubmittedJobInfoModelValidator());
        RuleForEach(x => x.TaskFileOffsets).SetValidator(new TaskFileOffsetExtValidator());
    }
}