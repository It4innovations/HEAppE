using HEAppE.DataAccessTier.IRepository.ClusterInformation;
using HEAppE.DataAccessTier.IRepository.FileTransfer;
using HEAppE.DataAccessTier.IRepository.JobManagement;
using HEAppE.DataAccessTier.IRepository.JobManagement.Command;
using HEAppE.DataAccessTier.IRepository.JobManagement.JobInformation;
using HEAppE.DataAccessTier.IRepository.OpenStack;
using HEAppE.DataAccessTier.IRepository.UserAndLimitationManagement;
using System;

namespace HEAppE.DataAccessTier.UnitOfWork
{
    public interface IUnitOfWork : IDisposable
    {
        #region Properties
        bool IsDisposed { get; }
        #endregion
        #region Repositories
        IClusterAuthenticationCredentialsRepository ClusterAuthenticationCredentialsRepository { get; }
        IClusterRepository ClusterRepository { get; }
        IProjectRepository ProjectRepository { get; }
        ISubProjectRepository SubProjectRepository { get; }
        IContactRepository ContactRepository { get; }
        IClusterProjectRepository ClusterProjectRepository { get; }
        IClusterProxyConnectionRepository ClusterProxyConnectionRepository { get; }
        IClusterNodeTypeRepository ClusterNodeTypeRepository { get; }
        IClusterNodeTypeRequestedGroupRepository ClusterNodeTypeRequestedGroupRepository { get; }
        IClusterNodeTypeAggregationRepository ClusterNodeTypeAggregationRepository { get; }
        IClusterNodeTypeAggregationAccountingRepository ClusterNodeTypeAggregationAccountingRepository { get; }
        IOpenStackAuthenticationCredentialsRepository OpenStackAuthenticationCredentialsRepository { get; }
        IOpenStackDomainRepository OpenStackDomainRepository { get; }
        IOpenStackInstanceRepository OpenStackInstanceRepository { get; }
        IOpenStackProjectRepository OpenStackProjectRepository { get; }
        IEnvironmentVariableRepository EnvironmentVariableRepository { get; }
        IFileTransferMethodRepository FileTransferMethodRepository { get; }
        IFileTransferTemporaryKeyRepository FileTransferTemporaryKeyRepository { get; }
        IFileSpecificationRepository FileSpecificationRepository { get; }
        IAccountingRepository AccountingRepository { get; }
        ISubmittedJobInfoRepository SubmittedJobInfoRepository { get; }
        ISubmittedTaskInfoRepository SubmittedTaskInfoRepository { get; }
        ISubmittedTaskAllocationNodeInfoRepository SubmittedTaskAllocationNodeInfoRepository { get; }
        ICommandTemplateRepository CommandTemplateRepository { get; }
        ICommandTemplateParameterRepository CommandTemplateParameterRepository { get; }
        ICommandTemplateParameterValueRepository CommandTemplateParameterValueRepository { get; }
        IJobSpecificationRepository JobSpecificationRepository { get; }
        ITaskSpecificationRepository TaskSpecificationRepository { get; }
        ITaskParalizationSpecificationRepository TaskParalizationSpecificationRepository { get; }
        ITaskSpecificationRequiredNodeRepository TaskSpecificationRequiredNodeRepository { get; }
        IAdaptorUserRepository AdaptorUserRepository { get; }
        IAdaptorUserGroupRepository AdaptorUserGroupRepository { get; }
        IAdaptorUserRoleRepository AdaptorUserRoleRepository { get; }
        ISessionCodeRepository SessionCodeRepository { get; }
        IOpenStackSessionRepository OpenStackSessionRepository { get; }
        #endregion
        #region Methods
        void Save();
        #endregion
    }
}