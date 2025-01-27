using System.ComponentModel;
using System.Runtime.Serialization;
using HEAppE.RestApiModels.AbstractModels;

namespace HEAppE.RestApiModels.JobManagement;

/// <summary>
/// Model to delete job
/// </summary>
[DataContract(Name = "DeleteJobModel")]
[Description("Model to delete job")]
public class DeleteJobModel : SubmittedJobInfoModel
{
    [DataMember(Name = "archiveLogs")]
    public bool ArchiveLogs { get; set; } = false;
    public override string ToString()
    {
        return $"DeleteJobModel(ArchiveLogs: {ArchiveLogs}{base.ToString()})";
    }
}