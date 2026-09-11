using System.Runtime.Serialization;

namespace HEAppE.ExtModels.JobManagement.Models;

[DataContract(Name = "QSchedulerJobSpecificationExt")]
public class QSchedulerJobSpecificationExt
{
    [DataMember(Name = "Name")]
    public string Name { get; set; }

    [DataMember(Name = "ClusterId")]
    public long ClusterId { get; set; }

    [DataMember(Name = "ProjectId")]
    public long ProjectId { get; set; }

    [DataMember(Name = "Tasks")]
    public QSchedulerTaskSpecificationExt[] Tasks { get; set; }
}
