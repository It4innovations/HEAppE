using System.Runtime.Serialization;
using HEAppE.RestApiModels.AbstractModels;

namespace HEAppE.RestApiModels.FileTransfer;

[DataContract(Name = "ListChangedFilesForJobModel")]
public class ListChangedFilesForJobModel : SubmittedJobInfoModel
{
    public override string ToString()
    {
        return $"ListChangedFilesForJobModel({base.ToString()})";
    }
}