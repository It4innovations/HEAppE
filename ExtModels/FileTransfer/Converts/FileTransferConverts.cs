using HEAppE.BusinessLogicTier.Logic;
using HEAppE.DomainObjects.FileTransfer;
using HEAppE.ExtModels.ClusterInformation.Converts;
using HEAppE.ExtModels.FileTransfer.Models;
using HEAppE.ExtModels.UserAndLimitationManagement.Converts;
using System;

namespace HEAppE.ExtModels.FileTransfer.Converts
{
    public static class FileTransferConverts
    {
        #region Public Methods
        public static CustomFileTransferMethodExt ConvertCustomIntToExt(this FileTransferMethod fileTransferMethod)
        {
            var convert = new CustomFileTransferMethodExt()
            {
                ServerHostname = fileTransferMethod.ServerHostname,
                SharedBasepath = fileTransferMethod.SharedBasePath,
                Protocol = ConvertFileTransferProtocolIntToExt(fileTransferMethod.Protocol),
                ProxyConnection = fileTransferMethod.Cluster.ProxyConnection?.ConvertIntToExt(),
                FileTransferCipherType = ConvertFileTransferMethodIntToExt(fileTransferMethod.FileTransferCipherType),
                Credentials = fileTransferMethod.Credentials.ConvertIntToExt()
            };
            return convert;
        }
        
        public static FileTransferMethodExt ConvertIntToExt(this FileTransferMethod fileTransferMethod)
        {
            var convert = new FileTransferMethodExt()
            {
                ServerHostname = fileTransferMethod.ServerHostname,
                SharedBasepath = fileTransferMethod.SharedBasePath,
                Protocol = ConvertFileTransferProtocolIntToExt(fileTransferMethod.Protocol),
                ProxyConnection = fileTransferMethod.Cluster.ProxyConnection?.ConvertIntToExt(),
                FileTransferCipherType = ConvertFileTransferMethodIntToExt(fileTransferMethod.FileTransferCipherType),
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

        public static FileTransferMethod ConvertFileTransferMethodExtToInt(FileTransferMethodExt usedTransferMethod)
        {
            var convert = new FileTransferMethod
            {
                Credentials = usedTransferMethod.Credentials.ConvertExtToInt(),
                Protocol = ConvertFileTransferProtocolExtToInt(usedTransferMethod.Protocol),
                FileTransferCipherType = ConvertFileTransferMethodExtToInt(usedTransferMethod.FileTransferCipherType),
                ServerHostname = usedTransferMethod.ServerHostname,
                SharedBasePath = usedTransferMethod.SharedBasepath
            };
            return convert;
        }

        public static JobFileContentExt ConvertJobFileContentToExt(JobFileContent content)
        {
            var convert = new JobFileContentExt()
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

        private static FileTransferProtocol ConvertFileTransferProtocolExtToInt(FileTransferProtocolExt? protocol)
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

        private static FileTransferCipherTypeExt ConvertFileTransferMethodIntToExt(FileTransferCipherType fileTransferMethod)
        {
            return fileTransferMethod switch
            {
                FileTransferCipherType.RSA3072 => FileTransferCipherTypeExt.RSA3072,
                FileTransferCipherType.RSA4096 => FileTransferCipherTypeExt.RSA4096,
                FileTransferCipherType.nistP256 => FileTransferCipherTypeExt.nistP256,
                FileTransferCipherType.nistP521 => FileTransferCipherTypeExt.nistP521,
                FileTransferCipherType.Ed25519 => FileTransferCipherTypeExt.Ed25519,
                _ => FileTransferCipherTypeExt.RSA4096
            };
        }

        private static FileTransferCipherType ConvertFileTransferMethodExtToInt(FileTransferCipherTypeExt? fileTransferMethod)
        {
            if (!fileTransferMethod.HasValue)
            {
                throw new InputValidationException("The file transfer method has to be set.");
            }

            return fileTransferMethod switch
            {
                FileTransferCipherTypeExt.RSA3072 => FileTransferCipherType.RSA3072,
                FileTransferCipherTypeExt.RSA4096 => FileTransferCipherType.RSA4096,
                FileTransferCipherTypeExt.nistP256 => FileTransferCipherType.nistP256,
                FileTransferCipherTypeExt.nistP521 => FileTransferCipherType.nistP521,
                FileTransferCipherTypeExt.Ed25519 => FileTransferCipherType.Ed25519,
                _ => FileTransferCipherType.RSA4096
            };
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

        private static SynchronizableFiles ConvertSynchronizableFilesExtToInt(SynchronizableFilesExt? fileType)
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
        #endregion
    }
}
