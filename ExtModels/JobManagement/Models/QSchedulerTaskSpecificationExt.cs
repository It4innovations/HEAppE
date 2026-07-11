using System.Runtime.Serialization;

namespace HEAppE.ExtModels.JobManagement.Models;

[DataContract(Name = "QSchedulerTaskSpecificationExt")]
public class QSchedulerTaskSpecificationExt
{
    [DataMember(Name = "Name")]
    public string Name { get; set; }

    [DataMember(Name = "MachineId")]
    public string MachineId { get; set; }

    [DataMember(Name = "WalltimeLimitSecs")]
    public int WalltimeLimitSecs { get; set; }

    [DataMember(Name = "SessionId")]
    public long? SessionId { get; set; }

    [DataMember(Name = "PayloadPartName")]
    public string PayloadPartName { get; set; }

    [DataMember(Name = "PayloadFilePath")]
    public string PayloadFilePath { get; set; }

    [DataMember(Name = "PayloadContent")]
    public string PayloadContent { get; set; }
}
