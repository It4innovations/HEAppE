using System;
using HEAppE.DomainObjects.FileTransfer;
using HEAppE.Exceptions.External;
using HEAppE.ExtModels.ClusterInformation.Converts;
using HEAppE.ExtModels.FileTransfer.Models;
using HEAppE.ExtModels.UserAndLimitationManagement.Converts;

namespace HEAppE.ExtModels.FileTransfer.Converts;

public static class FileTransferConverts
{
    #region Public Methods

    public static FileTransferMethodExt ConvertIntToExt(this FileTransferMethod fileTransferMethod)
    {
        var convert = new FileTransferMethodExt
        {
            Id = fileTransferMethod.Id,
            ServerHostname = fileTransferMethod.ServerHostname,
            SharedBasepath = fileTransferMethod.SharedBasePath,
            Protocol = ConvertFileTransferProtocolIntToExt(fileTransferMethod.Protocol),
            Port = fileTransferMethod.Port,
            ProxyConnection = fileTransferMethod.Cluster?.ProxyConnection?.ConvertIntToExt(),
            Credentials = fileTransferMethod.Credentials.ConvertIntToExt()
        };
        return convert;
    }

    public static FileInformationExt ConvertIntToExt(this FileInformation fileInformation)
    {
        var convert = new FileInformationExt
        {
            FileName = fileInformation.FileName,
            LastModifiedDate = fileInformation.LastModifiedDate
        };
        return convert;
    }

    public static JobFileContentExt ConvertJobFileContentToExt(JobFileContent content)
    {
        var convert = new JobFileContentExt
        {
            Content = content.Content,
            RelativePath = content.RelativePath,
            Offset = content.Offset,
            FileType = ConvertSynchronizableFilesToExt(content.FileType),
            SubmittedTaskInfoId = content.SubmittedTaskInfoId
        };
        return convert;
    }

    public static TaskFileOffset ConvertTaskFileOffsetExtToInt(TaskFileOffsetExt taskFileOffset)
    {
        var convert = new TaskFileOffset
        {
            SubmittedTaskInfoId = taskFileOffset.SubmittedTaskInfoId ?? 0,
            Offset = taskFileOffset.Offset ?? 0,
            FileType = ConvertSynchronizableFilesExtToInt(taskFileOffset.FileType)
        };
        return convert;
    }

    #endregion

    #region Private Methods

    private static FileTransferProtocolExt ConvertFileTransferProtocolIntToExt(
        FileTransferProtocol? fileTransferProtocol)
    {
        if (!fileTransferProtocol.HasValue)
            throw new InputValidationException("EnumValueMustBeSet", "File transfer protocol");

        if (!Enum.TryParse(fileTransferProtocol.ToString(), out FileTransferProtocolExt convert))
            throw new InputValidationException("EnumValueMustBeInInterval", "File transfer protocol", "<1, 2, 4>");

        return convert;
    }

    private static FileTransferProtocol ConvertFileTransferProtocolExtToInt(FileTransferProtocolExt? protocol)
    {
        if (!protocol.HasValue) throw new InputValidationException("EnumValueMustBeSet", "File transfer protocol");

        if (!Enum.TryParse(protocol.ToString(), out FileTransferProtocol convert))
            throw new InputValidationException("EnumValueMustBeInInterval", "File transfer protocol", "<1, 2, 4>");
        return convert;
    }

    private static SynchronizableFilesExt? ConvertSynchronizableFilesToExt(SynchronizableFiles? fileType)
    {
        if (!fileType.HasValue) throw new InputValidationException("EnumValueMustBeSet", "Synchronizable file type");

        if (!Enum.TryParse(fileType.ToString(), out SynchronizableFilesExt convert))
            throw new InputValidationException("EnumValueMustBeInInterval", "Synchronizable file type", "<0, 1, 2, 3>");
        return convert;
    }

    private static SynchronizableFiles ConvertSynchronizableFilesExtToInt(SynchronizableFilesExt? fileType)
    {
        if (!fileType.HasValue) throw new InputValidationException("EnumValueMustBeSet", "Synchronizable file type");

        if (!Enum.TryParse(fileType.ToString(), out SynchronizableFiles convert))
            throw new InputValidationException("EnumValueMustBeInInterval", "Synchronizable file type", "<0, 1, 2, 3>");
        return convert;
    }

    #endregion
}