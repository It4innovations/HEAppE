using System.Runtime.Serialization;

namespace HEAppE.ExtModels.JobManagement.Models
{
    [DataContract(Name = "JobSpecificationByProjectExt")]
    public class JobSpecificationByProjectExt : JobSpecificationExt
    {
        [DataMember(Name = "ProjectId", IsRequired = false)]
        public long? ProjectId { get; set; }
        public override string ToString()
        {
            return $"JobSpecificationExt(name={Name}; project={ProjectId}; waitingLimit={WaitingLimit}; walltimeLimit={WalltimeLimit}; notificationEmail={NotificationEmail}; phoneNumber={PhoneNumber}; notifyOnAbort={NotifyOnAbort}; notifyOnFinish={NotifyOnFinish}; notifyOnStart={NotifyOnStart}; clusterId={ClusterId}; fileTransferMethodId={FileTransferMethodId}; environmentVariables={EnvironmentVariables}; tasks={Tasks})";
        }
    }
}
