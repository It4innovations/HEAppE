﻿using System.Runtime.Serialization;

namespace HEAppE.ExtModels.FileTransfer.Models
{
    [DataContract(Name = "TaskFileOffsetExt")]
    public class TaskFileOffsetExt
    {
        [DataMember(Name = "SubmittedTaskInfoId")]
        public long? SubmittedTaskInfoId { get; set; }

        [DataMember(Name = "FileType")]
        public SynchronizableFilesExt? FileType { get; set; }

        [DataMember(Name = "Offset")]
        public long? Offset { get; set; }

        public override string ToString()
        {
            return $"TaskFileOffsetExt(submittedTaskInfoId={SubmittedTaskInfoId}; fileType={FileType}; offset={Offset})";
        }
    }
}
