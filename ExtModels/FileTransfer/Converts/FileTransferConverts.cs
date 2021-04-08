using HEAppE.DomainObjects.FileTransfer;
using HEAppE.ExtModels.FileTransfer.Models;
using HEAppE.ExtModels.UserAndLimitationManagement.Converts;
using System;

namespace HEAppE.ExtModels.FileTransfer.Converts
{
    public static class FileTransferConverts
    {
        public static FileTransferMethodExt ConvertIntToExt(this FileTransferMethod fileTransferMethod)
        {
            FileTransferMethodExt convert = new FileTransferMethodExt()
            {
                ServerHostname = fileTransferMethod.ServerHostname,
                SharedBasepath = fileTransferMethod.SharedBasePath,
                Protocol = ConvertFileTransferProtocolToExt(fileTransferMethod.Protocol),
                Credentials = fileTransferMethod.Credentials.ConvertIntToExt()
            };
            return convert;
        }


        public static FileInformationExt ConvertIntToExt(this FileInformation fileInformation)
        {
            FileInformationExt convert = new FileInformationExt
            {
                FileName = fileInformation.FileName,
                LastModifiedDate = fileInformation.LastModifiedDate
            };
            return convert;
        }

        private static FileTransferProtocolExt ConvertFileTransferProtocolToExt(FileTransferProtocol? fileTransferProtocol)
        {
            FileTransferProtocolExt convert;
            if (!fileTransferProtocol.HasValue)
                throw new Exception("The file transfer protocol has to be set.");
#warning InputValidationExceptionExt
            Enum.TryParse(fileTransferProtocol.ToString(), out convert);
            return convert;
        }

        public static FileTransferMethod ConvertFileTransferMethodExtToIn(FileTransferMethodExt usedTransferMethod)
        {
            FileTransferMethod convert = new FileTransferMethod
            {
                Credentials = usedTransferMethod.Credentials.ConvertExtToInt(),
                Protocol = ConvertFileTransferProtocolExtToIn(usedTransferMethod.Protocol),
                ServerHostname = usedTransferMethod.ServerHostname,
                SharedBasePath = usedTransferMethod.SharedBasepath
            };
            return convert;
        }

        private static FileTransferProtocol ConvertFileTransferProtocolExtToIn(FileTransferProtocolExt? protocol)
        {
            FileTransferProtocol convert;
            Enum.TryParse(protocol.ToString(), out convert);
            return convert;
        }

        public static JobFileContentExt ConvertJobFileContentToExt(JobFileContent content)
        {
            JobFileContentExt convert = new JobFileContentExt()
            {
                Content = content.Content,
                RelativePath = content.RelativePath,
                Offset = content.Offset,
                FileType = ConvertSynchronizableFilesToExt(content.FileType),
                SubmittedTaskInfoId = content.SubmittedTaskInfoId
            };
            return convert;
        }

        private static SynchronizableFilesExt? ConvertSynchronizableFilesToExt(SynchronizableFiles? fileType)
        {
            if (!fileType.HasValue)
                return null;
            SynchronizableFilesExt convert;
            Enum.TryParse(fileType.ToString(), out convert);
            return convert;
        }

        public static TaskFileOffset ConvertTaskFileOffsetExtToIn(TaskFileOffsetExt taskFileOffset)
        {
            TaskFileOffset convert = new TaskFileOffset
            {
                SubmittedTaskInfoId = taskFileOffset.SubmittedTaskInfoId ?? 0,
                Offset = taskFileOffset.Offset ?? 0,
                FileType = ConvertSynchronizableFilesExtToIn(taskFileOffset.FileType)
            };
            return convert;
        }

        private static SynchronizableFiles ConvertSynchronizableFilesExtToIn(SynchronizableFilesExt? fileType)
        {
            SynchronizableFiles convert;
            if (!fileType.HasValue)
                throw new Exception("The synchronizable file type has to be set.");
#warning InputValidationExceptionExt
            Enum.TryParse(fileType.ToString(), out convert);
            return convert;
        }
    }
}
