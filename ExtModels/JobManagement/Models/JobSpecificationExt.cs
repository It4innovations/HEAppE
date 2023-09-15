using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace HEAppE.ExtModels.JobManagement.Models
{
    [DataContract(Name = "JobSpecificationExt")]
    public class JobSpecificationExt
    {
        [DataMember(Name = "Name"), StringLength(50)]
        public string Name { get; set; }
        
        [DataMember(Name = "ProjectId", IsRequired = true)]
        public long ProjectId { get; set; }

        [DataMember(Name = "WaitingLimit")]
        public int? WaitingLimit { get; set; }

        [DataMember(Name = "WalltimeLimit")]
        public int? WalltimeLimit { get; }

        [DataMember(Name = "NotificationEmail"), StringLength(50)]
        public string NotificationEmail { get; set; }

        [DataMember(Name = "PhoneNumber"), StringLength(20)]
        public string PhoneNumber { get; set; }

        [DataMember(Name = "NotifyOnAbort")]
        public bool? NotifyOnAbort { get; set; }

        [DataMember(Name = "NotifyOnFinish")]
        public bool? NotifyOnFinish { get; set; }

        [DataMember(Name = "NotifyOnStart")]
        public bool? NotifyOnStart { get; set; }

        [DataMember(Name = "ClusterId")]
        public long? ClusterId { get; set; }

        [DataMember(Name = "FileTransferMethodId")]
        public long? FileTransferMethodId { get; set; }

        [DataMember(Name = "IsExtraLong")]
        public bool IsExtraLong { get; set; } = false;

        [DataMember(Name = "EnvironmentVariables")]
        public EnvironmentVariableExt[] EnvironmentVariables { get; set; }

        [DataMember(Name = "Tasks")]
        public TaskSpecificationExt[] Tasks { get; set; }
        public override string ToString()
        {
            return $"JobSpecificationExt(name={Name}; project={ProjectId}; waitingLimit={WaitingLimit}; walltimeLimit={WalltimeLimit}; notificationEmail={NotificationEmail}; phoneNumber={PhoneNumber}; notifyOnAbort={NotifyOnAbort}; notifyOnFinish={NotifyOnFinish}; notifyOnStart={NotifyOnStart}; clusterId={ClusterId}; fileTransferMethodId={FileTransferMethodId}; environmentVariables={EnvironmentVariables}; tasks={Tasks})";
        }
    }
}