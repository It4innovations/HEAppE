using System;
using System.Collections.Generic;

namespace HEAppE.DomainObjects.FileTransfer;

public class SynchronizedJobFiles
{
    public long SubmittedJobInfoId { get; set; }
    public DateTime SynchronizationTime { get; set; }
    public List<JobFileContent> FileContents { get; set; } = new();
}