using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.IO;
using System.Text;
using System.Text.Json;
using HEAppE.DomainObjects.ClusterInformation;
using HEAppE.DomainObjects.FileTransfer;
using HEAppE.DomainObjects.UserAndLimitationManagement;

namespace HEAppE.DomainObjects.JobManagement {
	[Table("JobSpecification")]
	public class JobSpecification : CommonJobProperties {
        public int? WaitingLimit { get; set; }

		[StringLength(50)]
		public string NotificationEmail { get; set; }

		[StringLength(20)]
		public string PhoneNumber { get; set; }

		public bool? NotifyOnAbort { get; set; }

		public bool? NotifyOnFinish { get; set; }

		public bool? NotifyOnStart { get; set; }

        public virtual AdaptorUser Submitter { get; set; }

        public virtual AdaptorUserGroup SubmitterGroup { get; set; }

		[ForeignKey("ClusterId")]
		public long ClusterId { get; set; }
		public virtual Cluster Cluster { get; set; }

        [ForeignKey("FileTransferMethod")]
        public long? FileTransferMethodId { get; set; }
        public virtual FileTransferMethod FileTransferMethod { get; set; }

        public virtual List<TaskSpecification> Tasks { get; set; } = new List<TaskSpecification>();

		public virtual ClusterAuthenticationCredentials ClusterUser { get; set; }

		public override string ToString() {
			StringBuilder result = new StringBuilder("JobSpecification: " + base.ToString());
			result.AppendLine("WaitingLimit=" + WaitingLimit);
			result.AppendLine("NotificationEmail=" + NotificationEmail);
			result.AppendLine("PhoneNumber=" + PhoneNumber);
			result.AppendLine("NotifyOnAbort=" + NotifyOnAbort);
			result.AppendLine("NotifyOnFinish=" + NotifyOnFinish);
			result.AppendLine("NotifyOnStart=" + NotifyOnStart);
			result.AppendLine("Submitter=" + Submitter);
			result.AppendLine("SubmitterGroup=" + SubmitterGroup);
			result.AppendLine("Cluster=" + Cluster);
            result.AppendLine("FileTransferMethod=" + FileTransferMethod);
            result.AppendLine("ClusterUser=" + ClusterUser);
			int i = 0;
			foreach (TaskSpecification task in Tasks) {
				result.AppendLine("Task" + (i++) + ":" + task);
			}
			return result.ToString();
		}

        public string ConvertToLocalHPCInfo()
        {
            string output = string.Empty;
            using (MemoryStream ms = new MemoryStream())
            {
                using (Utf8JsonWriter writer = new Utf8JsonWriter(ms))
                {
                    writer.WriteStartArray();
                    writer.WriteStartObject();
                        writer.WritePropertyName(nameof(Id));
                        writer.WriteNumberValue(Id);

                        writer.WritePropertyName("StartTime");
                        writer.WriteStringValue("null");
                        
                        writer.WritePropertyName("EndTime");
                        writer.WriteStringValue("null");

                        writer.WritePropertyName("State");
                        writer.WriteStringValue("S");


                    /*writer.WriteStartArray();

                    /*foreach (var task in Tasks)
                    {
                        writer.WriteStartObject();

                        //reflection?
                        writer.WritePropertyName(nameof(customer.Id));
                        writer.WriteNumberValue(customer.Id);

                        writer.WritePropertyName(nameof(customer.Name));
                        writer.WriteStringValue(customer.Name);

                        writer.WritePropertyName(nameof(customer.Age));
                        writer.WriteNumberValue(customer.Age);

                        writer.WriteEndObject();
                    }#1#

                    */
                    writer.WriteEndObject();
                    writer.WriteEndArray();
                }

                output = Encoding.UTF8.GetString(ms.ToArray());
            }
            return output;
        }
	}
}