using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using HEAppE.RestApiModels.AbstractModels;

namespace HEAppE.RestApiModels.JobManagement;

[DataContract(Name = "DeleteJobModel")]
public class DeleteJobModel : SubmittedJobInfoModel
{
    [DataMember(Name = "archiveLogs")]
    public bool ArchiveLogs { get; set; } = false;
    public override string ToString()
    {
        return $"DeleteJobModel(ArchiveLogs: {ArchiveLogs}{base.ToString()})";
    }
}