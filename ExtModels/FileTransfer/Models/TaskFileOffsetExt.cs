using System.ComponentModel;
using System.Runtime.Serialization;
using FluentValidation;

namespace HEAppE.ExtModels.FileTransfer.Models;

/// <summary>
/// Task file model
/// </summary>
[DataContract(Name = "TaskFileOffsetExt")]
[Description("Task file model")]
public class TaskFileOffsetExt
{
    /// <summary>
    /// Submitted task info id
    /// </summary>
    [DataMember(Name = "SubmittedTaskInfoId")]
    [Description("Submitted task info id")]
    public long? SubmittedTaskInfoId { get; set; }

    /// <summary>
    /// File type
    /// </summary>
    [DataMember(Name = "FileType")]
    [Description("File type")]
    public SynchronizableFilesExt? FileType { get; set; }

    /// <summary>
    /// Offset
    /// </summary>
    [DataMember(Name = "Offset")]
    [Description("Offset")]
    public long? Offset { get; set; }

    public override string ToString()
    {
        return $"TaskFileOffsetExt(submittedTaskInfoId={SubmittedTaskInfoId}; fileType={FileType}; offset={Offset})";
    }
}

public class TaskFileOffsetExtValidator : AbstractValidator<TaskFileOffsetExt>
{
    public TaskFileOffsetExtValidator()
    {
        RuleFor(x => x.SubmittedTaskInfoId).NotNull().GreaterThan(0);
        RuleFor(x => x.FileType).NotNull().IsInEnum();
        RuleFor(x => x.Offset).NotNull().GreaterThan(0);
    }
}