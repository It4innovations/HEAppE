using HEAppE.BusinessLogicTier.Logic;
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
                Protocol = ConvertFileTransferProtocolIntToExt(fileTransferMethod.Protocol),
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

        private static FileTransferProtocolExt ConvertFileTransferProtocolIntToExt(FileTransferProtocol? fileTransferProtocol)
        {
            if (!fileTransferProtocol.HasValue)
            {
                throw new InputValidationException("The file transfer protocol has to be set.");
            }

            if (!Enum.TryParse(fileTransferProtocol.ToString(), out FileTransferProtocolExt convert))
            {
                throw new InputValidationException("The file transfer protocol type must have value from <1, 2, 4>.");
            }

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
            if (!protocol.HasValue)
            {
                throw new InputValidationException("The file transfer protocol has to be set.");
            }

            if (!Enum.TryParse(protocol.ToString(), out FileTransferProtocol convert))
            {
                throw new InputValidationException("The file transfer protocol type must have value from <1, 2, 4>.");
            }
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
            {
                throw new InputValidationException("The synchronizable file type has to be set.");
            }

            if (!Enum.TryParse(fileType.ToString(), out SynchronizableFilesExt convert))
            {
                throw new InputValidationException("The synchronizable file type must have value from range 0 to 3.");
            }
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
            if (!fileType.HasValue)
            {
                throw new InputValidationException("The synchronizable file type has to be set.");
            }

            if (!Enum.TryParse(fileType.ToString(), out SynchronizableFiles convert))
            {
                throw new InputValidationException("The synchronizable file type must have value from range 0 to 3.");
            }
            return convert;
        }
    }
}
