namespace HEAppE.DomainObjects.FileTransfer
{
    public class JobFileContent
    {
        public string Content { get; set; }
        public string RelativePath { get; set; }
        public long Offset { get; set; }
        public SynchronizableFiles? FileType { get; set; }
        public long? SubmittedTaskInfoId { get; set; }
    }
}