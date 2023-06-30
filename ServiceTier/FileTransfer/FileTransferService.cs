﻿using HEAppE.BusinessLogicTier.Factory;
using HEAppE.BusinessLogicTier.Logic;
using HEAppE.BusinessLogicTier.Logic.FileTransfer;
using HEAppE.DataAccessTier.Factory.UnitOfWork;
using HEAppE.DataAccessTier.UnitOfWork;
using HEAppE.DomainObjects.FileTransfer;
using HEAppE.DomainObjects.UserAndLimitationManagement;
using HEAppE.DomainObjects.UserAndLimitationManagement.Enums;
using HEAppE.ExtModels.FileTransfer.Converts;
using HEAppE.ExtModels.FileTransfer.Models;
using HEAppE.ServiceTier.UserAndLimitationManagement;
using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace HEAppE.ServiceTier.FileTransfer
{
    public class FileTransferService : IFileTransferService
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public FileTransferMethodExt TrustfulRequestFileTransfer(long submittedJobInfoId, string sessionCode)
        {
            try
            {
                using (IUnitOfWork unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
                {
                    AdaptorUser loggedUser = UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork, UserRoleType.Submitter, unitOfWork.SubmittedJobInfoRepository.GetById(submittedJobInfoId)?.Project.Id);
                    IFileTransferLogic fileTransferLogic = LogicFactory.GetLogicFactory().CreateFileTransferLogic(unitOfWork);
                    FileTransferMethod fileTransferMethod = fileTransferLogic.TrustfulRequestFileTransfer(submittedJobInfoId, loggedUser);
                    return fileTransferMethod.ConvertIntToExt();
                }
            }
            catch (Exception exc)
            {
                ExceptionHandler.ThrowProperExternalException(exc);
                return default;
            }
        }

        public FileTransferMethodExt RequestFileTransfer(long submittedJobInfoId, string sessionCode)
        {
            try
            {
                using (IUnitOfWork unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
                {
                    AdaptorUser loggedUser = UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork, UserRoleType.Submitter, unitOfWork.SubmittedJobInfoRepository.GetById(submittedJobInfoId)?.Project.Id);
                    IFileTransferLogic fileTransferLogic = LogicFactory.GetLogicFactory().CreateFileTransferLogic(unitOfWork);
                    FileTransferMethod fileTransferMethod = fileTransferLogic.GetFileTransferMethod(submittedJobInfoId, loggedUser);
                    return fileTransferMethod.ConvertIntToExt();
                }
            }
            catch (Exception exc)
            {
                ExceptionHandler.ThrowProperExternalException(exc);
                return default;
            }
        }

        public void CloseFileTransfer(long submittedJobInfoId, string publicKey, string sessionCode)
        {
            try
            {
                using (IUnitOfWork unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
                {
                    AdaptorUser loggedUser = UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork, UserRoleType.Submitter, unitOfWork.SubmittedJobInfoRepository.GetById(submittedJobInfoId)?.Project.Id);
                    IFileTransferLogic fileTransferLogic = LogicFactory.GetLogicFactory().CreateFileTransferLogic(unitOfWork);
                    fileTransferLogic.EndFileTransfer(submittedJobInfoId, publicKey, loggedUser);
                }
            }
            catch (Exception exc)
            {
                ExceptionHandler.ThrowProperExternalException(exc);
            }
        }

        public JobFileContentExt[] DownloadPartsOfJobFilesFromCluster(long submittedJobInfoId, TaskFileOffsetExt[] taskFileOffsets, string sessionCode)
        {
            try
            {
                using (IUnitOfWork unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
                {
                    AdaptorUser loggedUser = UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork, UserRoleType.Submitter, unitOfWork.SubmittedJobInfoRepository.GetById(submittedJobInfoId).Project.Id);
                    IFileTransferLogic fileTransferLogic = LogicFactory.GetLogicFactory().CreateFileTransferLogic(unitOfWork);
                    IList<JobFileContent> downloadedFileParts = fileTransferLogic.DownloadPartsOfJobFilesFromCluster(
                        submittedJobInfoId,
                        (from taskFileOffset in (new List<TaskFileOffsetExt>(taskFileOffsets).ToList()) select FileTransferConverts.ConvertTaskFileOffsetExtToInt(taskFileOffset)).ToArray(),
                        loggedUser);
                    return (from fileContent in downloadedFileParts select FileTransferConverts.ConvertJobFileContentToExt(fileContent)).ToArray();
                }
            }
            catch (Exception exc)
            {
                ExceptionHandler.ThrowProperExternalException(exc);
                return null;
            }
        }

        public FileInformationExt[] ListChangedFilesForJob(long submittedJobInfoId, string sessionCode)
        {
            try
            {
                using (IUnitOfWork unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
                {
                    AdaptorUser loggedUser = UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork, UserRoleType.Submitter, unitOfWork.SubmittedJobInfoRepository.GetById(submittedJobInfoId).Project.Id);
                    IFileTransferLogic fileTransferLogic = LogicFactory.GetLogicFactory().CreateFileTransferLogic(unitOfWork);
                    ICollection<FileInformation> result = fileTransferLogic.ListChangedFilesForJob(submittedJobInfoId, loggedUser);
                    return result?.Select(s => s.ConvertIntToExt()).ToArray();
                }
            }
            catch (Exception exc)
            {
                ExceptionHandler.ThrowProperExternalException(exc);
                return null;
            }
        }

        public byte[] DownloadFileFromCluster(long submittedJobInfoId, string relativeFilePath, string sessionCode)
        {
            try
            {
                using (IUnitOfWork unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
                {
                    AdaptorUser loggedUser = UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork, UserRoleType.Submitter, unitOfWork.SubmittedJobInfoRepository.GetById(submittedJobInfoId).Project.Id);
                    IFileTransferLogic fileTransferLogic = LogicFactory.GetLogicFactory().CreateFileTransferLogic(unitOfWork);
                    return fileTransferLogic.DownloadFileFromCluster(submittedJobInfoId, relativeFilePath, loggedUser);
                }
            }
            catch (Exception exc)
            {
                ExceptionHandler.ThrowProperExternalException(exc);
                return null;
            }
        }
    }
}