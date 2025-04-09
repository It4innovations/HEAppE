using System.ComponentModel;
using System.Runtime.Serialization;
using HEAppE.RestApiModels.AbstractModels;

namespace HEAppE.RestApiModels.FileTransfer;

/// <summary>
/// Model for retrieving list of changed files for job
/// </summary>
[DataContract(Name = "ListChangedFilesForJobModel")]
[Description("Model for retrieving list of changed files for job")]
public class ListChangedFilesForJobModel : SubmittedJobInfoModel
{
    public override string ToString()
    {
        return $"ListChangedFilesForJobModel({base.ToString()})";
    }
}