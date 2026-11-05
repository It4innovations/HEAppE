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

    [DataMember(Name = "UseSessions")]
    public bool? UseSessions { get; set; }

    /// <summary>
    /// What the payload describes: "circuit" (default, JSON) or "pulse" (protobuf RunDefinition).
    /// Pulse tasks are only accepted by machines with pulse-level access.
    /// </summary>
    [DataMember(Name = "Kind")]
    public string Kind { get; set; }

    public bool IsPulse() => string.Equals(Kind, "pulse", System.StringComparison.OrdinalIgnoreCase);

    /// <summary>File name the uploaded payload is stored under in the task directory.</summary>
    public string DefaultPayloadFileName() => IsPulse() ? "payload.pb" : "payload.json";
}
