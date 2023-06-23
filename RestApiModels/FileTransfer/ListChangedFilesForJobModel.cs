using HEAppE.RestApiModels.AbstractModels;
using System.Runtime.Serialization;

namespace HEAppE.RestApiModels.FileTransfer
{
    [DataContract(Name = "ListChangedFilesForJobModel")]
    public class ListChangedFilesForJobModel : SubmittedJobInfoModel
    {
        public override string ToString()
        {
            return $"ListChangedFilesForJobModel({base.ToString()})";
        }
    }
}
