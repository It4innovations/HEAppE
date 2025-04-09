using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using HEAppE.DomainObjects.ClusterInformation;
using HEAppE.DomainObjects.FileTransfer;
using HEAppE.DomainObjects.UserAndLimitationManagement;

namespace HEAppE.DomainObjects.JobManagement;

[Table("JobSpecification")]
public class JobSpecification : CommonJobProperties
{
    public int? WaitingLimit { get; set; }

    [StringLength(50)] public string NotificationEmail { get; set; }

    [StringLength(20)] public string PhoneNumber { get; set; }

    public bool? NotifyOnAbort { get; set; }

    public bool? NotifyOnFinish { get; set; }

    public bool? NotifyOnStart { get; set; }

    public virtual AdaptorUser Submitter { get; set; }

    public virtual AdaptorUserGroup SubmitterGroup { get; set; }

    [ForeignKey("ClusterId")] public long ClusterId { get; set; }

    public virtual Cluster Cluster { get; set; }

    [ForeignKey("FileTransferMethod")] public long? FileTransferMethodId { get; set; }

    public virtual FileTransferMethod FileTransferMethod { get; set; }

    [ForeignKey("SubProject")] public long? SubProjectId { get; set; }

    public virtual SubProject SubProject { get; set; }
    public virtual List<TaskSpecification> Tasks { get; set; } = new();

    public virtual ClusterAuthenticationCredentials ClusterUser { get; set; }

    public override string ToString()
    {
        var result = new StringBuilder("JobSpecification: " + base.ToString());
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
        var i = 0;
        foreach (var task in Tasks) result.AppendLine("Task" + i++ + ":" + task);
        return result.ToString();
    }

    public string ConvertToLocalHPCInfo(string jobState, string tasksState)
    {
        var output = string.Empty;
        using (var ms = new MemoryStream())
        {
            using (var writer = new Utf8JsonWriter(ms))
            {
                writer.WriteStartObject();
                writer.WritePropertyName(nameof(Id));
                writer.WriteNumberValue(Id);

                writer.WritePropertyName("SubmitTime");
                writer.WriteNullValue();

                writer.WritePropertyName("StartTime");
                writer.WriteNullValue();

                writer.WritePropertyName("EndTime");
                writer.WriteNullValue();

                writer.WritePropertyName("State");
                writer.WriteStringValue(jobState);

                writer.WritePropertyName("Name");
                writer.WriteStringValue(Name);

                writer.WritePropertyName("Project");
                writer.WriteStringValue(Project.AccountingString);

                writer.WritePropertyName("CreateTime");
                writer.WriteStringValue(DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss"));

                writer.WritePropertyName("Tasks");
                writer.WriteStartArray();


                foreach (var task in Tasks)
                {
                    writer.WriteStartObject();

                    writer.WritePropertyName(nameof(task.Id));
                    writer.WriteNumberValue(task.Id);

                    writer.WritePropertyName(nameof(task.Name));
                    writer.WriteStringValue(task.Id.ToString());

                    writer.WritePropertyName("State");
                    writer.WriteStringValue(tasksState);

                    writer.WritePropertyName("StartTime");
                    writer.WriteNullValue();

                    writer.WritePropertyName("EndTime");
                    writer.WriteNullValue();

                    writer.WritePropertyName("AllocatedTime");
                    writer.WriteNumberValue(0);

                    writer.WritePropertyName("JobArrays");
                    writer.WriteStringValue(task.JobArrays);

                    writer.WritePropertyName("DependsOn");
                    writer.WriteStringValue(string.Join(",",
                        task.DependsOn.Select(x => x.ParentTaskSpecificationId.ToString())));

                    writer.WriteEndObject();
                }


                writer.WriteEndArray();

                writer.WriteEndObject();
            }

            output = Encoding.UTF8.GetString(ms.ToArray());
        }

        return output;
    }
}