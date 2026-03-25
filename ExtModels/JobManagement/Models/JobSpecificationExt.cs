using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace HEAppE.ExtModels.JobManagement.Models;

/// <summary>
/// Job specification model
/// </summary>
[DataContract(Name = "JobSpecificationExt")]
[Description("Job specification model")]
public class JobSpecificationExt
{
    /// <summary>
    /// Name
    /// </summary>
    [DataMember(Name = "Name")]
    [StringLength(50)]
    [Description("Name")]
    public string Name { get; set; }

    /// <summary>
    /// Project id
    /// </summary>
    [DataMember(Name = "ProjectId", IsRequired = true)]
    [Description("Project id")]
    public long ProjectId { get; set; }

    /// <summary>
    /// Sub project identifier
    /// </summary>
    [DataMember(Name = "SubProjectIdentifier", IsRequired = false)]
    [StringLength(50)]
    [Description("Sub project identifier")]
    public string SubProjectIdentifier { get; set; }

    /// <summary>
    /// Waiting limit
    /// </summary>
    [DataMember(Name = "WaitingLimit")]
    [Description("Waiting limit")]
    public int? WaitingLimit { get; set; }

    /// <summary>
    /// Walltime limit
    /// </summary>
    [DataMember(Name = "WalltimeLimit")]
    [Description("Walltime limit")]
    public int? WalltimeLimit { get; }

    /// <summary>
    /// Notification email
    /// </summary>
    [DataMember(Name = "NotificationEmail")]
    [StringLength(50)]
    [Description("Notification email")]
    public string NotificationEmail { get; set; }

    /// <summary>
    /// Phone number
    /// </summary>
    [DataMember(Name = "PhoneNumber")]
    [StringLength(20)]
    [Description("Phone number")]
    public string PhoneNumber { get; set; }

    /// <summary>
    /// Notify on abort
    /// </summary>
    [DataMember(Name = "NotifyOnAbort")]
    [Description("Notify on abort")]
    public bool? NotifyOnAbort { get; set; }

    /// <summary>
    /// Notify on finish
    /// </summary>
    [DataMember(Name = "NotifyOnFinish")]
    [Description("Notify on finish")]
    public bool? NotifyOnFinish { get; set; }

    /// <summary>
    /// Notify on start
    /// </summary>
    [DataMember(Name = "NotifyOnStart")]
    [Description("Notify on start")]
    public bool? NotifyOnStart { get; set; }

    /// <summary>
    /// Cluster id
    /// </summary>
    [DataMember(Name = "ClusterId")]
    [Description("Cluster id")]
    public long? ClusterId { get; set; }

    /// <summary>
    /// File transfer method id
    /// </summary>
    [DataMember(Name = "FileTransferMethodId")]
    [Description("File transfer method id")]
    public long? FileTransferMethodId { get; set; }

    /// <summary>
    /// Is extra long
    /// </summary>
    [DataMember(Name = "IsExtraLong")]
    [Description("Is extra long")]
    public bool IsExtraLong { get; set; }

    /// <summary>
    /// Array of environment variables
    /// </summary>
    [DataMember(Name = "EnvironmentVariables")]
    [Description("Array of environment variables")]
    public EnvironmentVariableExt[] EnvironmentVariables { get; set; }

    /// <summary>
    /// Array of task specification models
    /// </summary>
    [DataMember(Name = "Tasks")]
    [Description("Array of task specification models")]
    public TaskSpecificationExt[] Tasks { get; set; }

    public override string ToString()
    {
        return
            $"JobSpecificationExt(name={Name}; project={ProjectId}; subProject={SubProjectIdentifier}; waitingLimit={WaitingLimit}; walltimeLimit={WalltimeLimit}; notificationEmail={NotificationEmail}; phoneNumber={PhoneNumber}; notifyOnAbort={NotifyOnAbort}; notifyOnFinish={NotifyOnFinish}; notifyOnStart={NotifyOnStart}; clusterId={ClusterId}; fileTransferMethodId={FileTransferMethodId}; environmentVariables={EnvironmentVariables}; tasks={Tasks})";
    }
}