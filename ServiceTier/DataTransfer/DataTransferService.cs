using HEAppE.BusinessLogicTier.Factory;
using HEAppE.BusinessLogicTier.Logic.DataTransfer;
using HEAppE.DataAccessTier.Factory.UnitOfWork;
using HEAppE.DataAccessTier.UnitOfWork;
using HEAppE.DomainObjects.DataTransfer;
using HEAppE.DomainObjects.UserAndLimitationManagement;
using HEAppE.DomainObjects.UserAndLimitationManagement.Enums;
using HEAppE.Exceptions.External;
using HEAppE.ExtModels.DataTransfer.Converts;
using HEAppE.ExtModels.DataTransfer.Models;
using HEAppE.ServiceTier.UserAndLimitationManagement;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HEAppE.ServiceTier.DataTransfer
{
    public class DataTransferService : IDataTransferService
    {
        public DataTransferMethodExt RequestDataTransfer(string nodeIPAddress, int nodePort, long submittedTaskInfoId, string sessionCode)
        {
            using (IUnitOfWork unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
            {
                var submittedTaskInfo = unitOfWork.SubmittedTaskInfoRepository.GetById(submittedTaskInfoId);
                if (submittedTaskInfo == null)
                {
                    throw new InputValidationException("Submitted task info not found");
                }
                AdaptorUser loggedUser = UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork, UserRoleType.Submitter, submittedTaskInfo.Project.Id);
                IDataTransferLogic dataTransferLogic = LogicFactory.GetLogicFactory().CreateDataTransferLogic(unitOfWork);
                DataTransferMethod dataTransferMethod = dataTransferLogic.GetDataTransferMethod(nodeIPAddress, nodePort, submittedTaskInfoId, loggedUser);
                return dataTransferMethod.ConvertIntToExt();
            }
        }

        public void CloseDataTransfer(DataTransferMethodExt usedTransferMethod, string sessionCode)
        {
            using (IUnitOfWork unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
            {
                var submittedTaskInfo = unitOfWork.SubmittedTaskInfoRepository.GetById(usedTransferMethod.SubmittedTaskId);
                if (submittedTaskInfo == null)
                {
                    throw new InputValidationException("Submitted task info not found");
                }
                AdaptorUser loggedUser = UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork, UserRoleType.Submitter, submittedTaskInfo.Project.Id);
                IDataTransferLogic dataTransferLogic = LogicFactory.GetLogicFactory().CreateDataTransferLogic(unitOfWork);
                dataTransferLogic.EndDataTransfer(usedTransferMethod.ConvertExtToInt(), loggedUser);
            }
        }

        public async Task<string> HttpGetToJobNodeAsync(string httpRequest, IEnumerable<HTTPHeaderExt> httpHeaders, long submittedTaskInfoId, string nodeIPAddress, int nodePort, string sessionCode)
        {
            using (IUnitOfWork unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
            {
                var submittedTaskInfo = unitOfWork.SubmittedTaskInfoRepository.GetById(submittedTaskInfoId);
                if (submittedTaskInfo == null)
                {
                    throw new InputValidationException("Submitted task info not found");
                }
                AdaptorUser loggedUser = UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork, UserRoleType.Submitter, submittedTaskInfo.Project.Id);
                IDataTransferLogic dataTransferLogic = LogicFactory.GetLogicFactory().CreateDataTransferLogic(unitOfWork);
                return await dataTransferLogic.HttpGetToJobNodeAsync(httpRequest, httpHeaders.Select(s => s.ConvertExtToInt()), submittedTaskInfoId, nodeIPAddress, nodePort, loggedUser);
            }
        }

        public async Task<string> HttpPostToJobNodeAsync(string httpRequest, IEnumerable<HTTPHeaderExt> httpHeaders, string httpPayload, long submittedTaskInfoId, string nodeIPAddress, int nodePort, string sessionCode)
        {
            using (IUnitOfWork unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
            {
                var submittedTaskInfo = unitOfWork.SubmittedTaskInfoRepository.GetById(submittedTaskInfoId);
                if (submittedTaskInfo == null)
                {
                    throw new InputValidationException("Submitted task info not found");
                }
                AdaptorUser loggedUser = UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork, UserRoleType.Submitter, submittedTaskInfo.Project.Id);
                IDataTransferLogic dataTransferLogic = LogicFactory.GetLogicFactory().CreateDataTransferLogic(unitOfWork);
                return await dataTransferLogic.HttpPostToJobNodeAsync(httpRequest, httpHeaders.Select(s => s.ConvertExtToInt()), httpPayload, submittedTaskInfoId, nodeIPAddress, nodePort, loggedUser);
            }
        }
    }
}