using System.ComponentModel;
using System.Runtime.Serialization;

namespace HEAppE.ExtModels.FileTransfer.Models;

/// <summary>
/// Job file content ext
/// </summary>
[DataContract(Name = "JobFileContentExt")]
[Description("Job file content ext")]
public class JobFileContentExt
{
    /// <summary>
    /// Content
    /// </summary>
    [DataMember(Name = "Content")]
    [Description("Content")]
    public string Content { get; set; }

    /// <summary>
    /// Relative path
    /// </summary>
    [DataMember(Name = "RelativePath")]
    [Description("Relative path")]
    public string RelativePath { get; set; }

    /// <summary>
    /// Offset
    /// </summary>
    [DataMember(Name = "Offset")]
    [Description("Offset")]
    public long? Offset { get; set; }

    /// <summary>
    /// File type
    /// </summary>
    [DataMember(Name = "FileType")]
    [Description("File type")]
    public SynchronizableFilesExt? FileType { get; set; }

    /// <summary>
    /// Submitted task info id
    /// </summary>
    [DataMember(Name = "SubmittedTaskInfoId")]
    [Description("Submitted task info id")]
    public long? SubmittedTaskInfoId { get; set; }

    public override string ToString()
    {
        return $"JobFileContentExt(content={Content}; relativePath={RelativePath}; offset={Offset}; fileType={FileType}; submittedTaskInfoId={SubmittedTaskInfoId})";
    }
}