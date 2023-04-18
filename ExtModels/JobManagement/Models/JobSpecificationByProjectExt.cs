using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace HEAppE.ExtModels.JobManagement.Models
{
    [DataContract(Name = "JobSpecificationByProjectExt")]
    public class JobSpecificationByProjectExt : JobSpecificationExt
    {
        [DataMember(Name = "ProjectId")]
        public long? ProjectId { get; set; }
        public override string ToString()
        {
            return $"JobSpecificationExt(name={Name}; project={ProjectId}; waitingLimit={WaitingLimit}; walltimeLimit={WalltimeLimit}; notificationEmail={NotificationEmail}; phoneNumber={PhoneNumber}; notifyOnAbort={NotifyOnAbort}; notifyOnFinish={NotifyOnFinish}; notifyOnStart={NotifyOnStart}; clusterId={ClusterId}; fileTransferMethodId={FileTransferMethodId}; environmentVariables={EnvironmentVariables}; tasks={Tasks})";
        }
    }
}
