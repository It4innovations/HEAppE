using System.Linq;
using System.Runtime.Serialization;
using FluentValidation;
using HEAppE.ExtModels.FileTransfer.Models;
using HEAppE.RestApiModels.AbstractModels;

namespace HEAppE.RestApiModels.FileTransfer;

[DataContract(Name = "DownloadPartsOfJobFilesFromClusterModel")]
public class DownloadPartsOfJobFilesFromClusterModel : SubmittedJobInfoModel
{
    [DataMember(Name = "TaskFileOffsets")] public TaskFileOffsetExt[] TaskFileOffsets { get; set; }

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