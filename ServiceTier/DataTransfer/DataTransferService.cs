using System;
using System.Collections.Generic;
using HEAppE.BusinessLogicTier.Factory;
using HEAppE.BusinessLogicTier.Logic.DataTransfer;
using HEAppE.DataAccessTier.Factory.UnitOfWork;
using HEAppE.DataAccessTier.UnitOfWork;
using HEAppE.DomainObjects.DataTransfer;
using HEAppE.DomainObjects.JobManagement.JobInformation;
using HEAppE.DomainObjects.UserAndLimitationManagement;
using HEAppE.ServiceTier.UserAndLimitationManagement;
using log4net;
using System.Reflection;
using HEAppE.ExtModels.DataTransfer.Models;
using HEAppE.ExtModels.DataTransfer.Converts;
using HEAppE.BusinessLogicTier.Logic;

namespace HEAppE.ServiceTier.DataTransfer
{
    public class DataTransferService : IDataTransferService
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public static List<string> publicList = new List<string>();

        public DataTransferMethodExt GetDataTransferMethod(string ipAddress, int port, long submittedJobInfoId, string sessionCode)
        {
            try
            {
                using (IUnitOfWork unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
                {
                    AdaptorUser loggedUser = UserAndLimitationManagementService.GetUserForSessionCode(sessionCode, unitOfWork);
                    SubmittedJobInfo jobInfo = LogicFactory.GetLogicFactory().CreateJobManagementLogic(unitOfWork).GetSubmittedJobInfoById(submittedJobInfoId, loggedUser);
                    IDataTransferLogic dataTransferLogic = LogicFactory.GetLogicFactory().CreateDataTransferLogic(unitOfWork);
                    DataTransferMethod dataTransferMethod = dataTransferLogic.GetDataTransferMethod(ipAddress, port, jobInfo, loggedUser);
                    return dataTransferMethod.ConvertIntToExt();
                }
            }
            catch (Exception exc)
            {
                ExceptionHandler.ThrowProperExternalException(exc);
                return default;
            }
        }

        public void EndDataTransfer(DataTransferMethodExt usedTransferMethod, string sessionCode)
        {
            try
            {
                using (IUnitOfWork unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
                {
                    AdaptorUser loggedUser = UserAndLimitationManagementService.GetUserForSessionCode(sessionCode, unitOfWork);
                    SubmittedJobInfo jobInfo = LogicFactory.GetLogicFactory().CreateJobManagementLogic(unitOfWork).GetSubmittedJobInfoById(usedTransferMethod.SubmittedJobId, loggedUser);
                    IDataTransferLogic dataTransferLogic = LogicFactory.GetLogicFactory().CreateDataTransferLogic(unitOfWork);
                    dataTransferLogic.EndDataTransfer(usedTransferMethod.ConvertExtToInt(), jobInfo, loggedUser);
                }
            }
            catch (Exception exc)
            {
                ExceptionHandler.ThrowProperExternalException(exc);
            }
        }

        public string HttpGetToJobNode(string httpRequest, string[] httpHeaders, long submittedJobInfoId, string ipAddress, string sessionCode)
        {
            try
            {
                using (IUnitOfWork unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
                {
                    AdaptorUser loggedUser = UserAndLimitationManagementService.GetUserForSessionCode(sessionCode, unitOfWork);
                    SubmittedJobInfo jobInfo = LogicFactory.GetLogicFactory().CreateJobManagementLogic(unitOfWork).GetSubmittedJobInfoById(submittedJobInfoId, loggedUser);
                    IDataTransferLogic dataTransferLogic = LogicFactory.GetLogicFactory().CreateDataTransferLogic(unitOfWork);
                    return dataTransferLogic.HttpGetToJobNode(httpRequest, httpHeaders, jobInfo.Id, ipAddress);
                }
            }
            catch (Exception exc)
            {
                ExceptionHandler.ThrowProperExternalException(exc);
                return default;
            }
        }

        public string HttpPostToJobNode(string httpRequest, string[] httpHeaders, string httpPayload, long submittedJobInfoId, string ipAddress, string sessionCode)
        {
            try
            {
                using (IUnitOfWork unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
                {
                    AdaptorUser loggedUser = UserAndLimitationManagementService.GetUserForSessionCode(sessionCode, unitOfWork);
                    SubmittedJobInfo jobInfo = LogicFactory.GetLogicFactory().CreateJobManagementLogic(unitOfWork).GetSubmittedJobInfoById(submittedJobInfoId, loggedUser);
                    IDataTransferLogic dataTransferLogic = LogicFactory.GetLogicFactory().CreateDataTransferLogic(unitOfWork);
                    return dataTransferLogic.HttpPostToJobNode(httpRequest, httpHeaders, httpPayload, jobInfo.Id, ipAddress);
                }
            }
            catch (Exception exc)
            {
                ExceptionHandler.ThrowProperExternalException(exc);
                return default;
            }
        }

        //public int? WriteDataToJobNode(byte[] data, long submittedJobInfoId, string ipAddress, bool closeConnection, string sessionCode)
        //{
        //    try
        //    {
        //        using (IUnitOfWork unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
        //        {
        //            AdaptorUser loggedUser = UserAndLimitationManagementService.GetUserForSessionCode(sessionCode, unitOfWork);
        //            SubmittedJobInfo jobInfo = LogicFactory.GetLogicFactory().CreateJobManagementLogic(unitOfWork).GetSubmittedJobInfoById(submittedJobInfoId, loggedUser);
        //            IDataTransferLogic dataTransferLogic = LogicFactory.GetLogicFactory().CreateDataTransferLogic(unitOfWork);
        //            return dataTransferLogic.WriteDataToJobNode(data, jobInfo.Id, ipAddress, sessionCode, closeConnection);
        //        }
        //    }
        //    catch (Exception exc)
        //    {
        //        ExceptionHandler.ThrowProperExternalException(exc);
        //        return default;
        //    }
        //}

        //public byte[] ReadDataFromJobNode(long submittedJobInfoId, string ipAddress, string sessionCode)
        //{
        //    try
        //    {
        //        using (IUnitOfWork unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
        //        {
        //            AdaptorUser loggedUser = UserAndLimitationManagementService.GetUserForSessionCode(sessionCode, unitOfWork);
        //            SubmittedJobInfo jobInfo = LogicFactory.GetLogicFactory().CreateJobManagementLogic(unitOfWork).GetSubmittedJobInfoById(submittedJobInfoId, loggedUser);
        //            IDataTransferLogic dataTransferLogic = LogicFactory.GetLogicFactory().CreateDataTransferLogic(unitOfWork);
        //            return dataTransferLogic.ReadDataFromJobNode(jobInfo.Id, ipAddress, sessionCode);
        //        }
        //    }
        //    catch (Exception exc)
        //    {
        //        ExceptionHandler.ThrowProperExternalException(exc);
        //        return default;
        //    }
        //}
    }
}