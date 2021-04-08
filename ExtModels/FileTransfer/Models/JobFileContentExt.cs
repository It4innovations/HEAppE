using System;
using System.Runtime.Serialization;

namespace HEAppE.ExtModels.FileTransfer.Models
{
    [DataContract(Name = "JobFileContentExt")]
    public class JobFileContentExt
    {
        [DataMember(Name = "Content")]
        public string Content { get; set; }

        [DataMember(Name = "RelativePath")]
        public string RelativePath { get; set; }

        [DataMember(Name = "Offset")]
        public long? Offset { get; set; }

        [DataMember(Name = "FileType")]
        public SynchronizableFilesExt? FileType { get; set; }

        [DataMember(Name = "SubmittedTaskInfoId")]
        public long? SubmittedTaskInfoId { get; set; }

        public override string ToString()
        {
            return $"JobFileContentExt(content={Content}; relativePath={RelativePath}; offset={Offset}; fileType={FileType}; submittedTaskInfoId={SubmittedTaskInfoId})";
        }
    }
}
