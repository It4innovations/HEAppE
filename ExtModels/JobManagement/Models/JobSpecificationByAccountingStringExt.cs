using System.Runtime.Serialization;

namespace HEAppE.ExtModels.JobManagement.Models
{
    [DataContract(Name = "JobSpecificationByAccountingStringExt")]
    public class JobSpecificationByAccountingStringExt : JobSpecificationExt
    {
        [DataMember(Name = "AccountingString", IsRequired = true)]
        public string AccountingString { get; set; }

        public override string ToString()
        {
            return $"JobSpecificationExt(name={Name}; accountingString={AccountingString}; waitingLimit={WaitingLimit}; walltimeLimit={WalltimeLimit}; notificationEmail={NotificationEmail}; phoneNumber={PhoneNumber}; notifyOnAbort={NotifyOnAbort}; notifyOnFinish={NotifyOnFinish}; notifyOnStart={NotifyOnStart}; clusterId={ClusterId}; fileTransferMethodId={FileTransferMethodId}; environmentVariables={EnvironmentVariables}; tasks={Tasks})";
        }
    }
}
