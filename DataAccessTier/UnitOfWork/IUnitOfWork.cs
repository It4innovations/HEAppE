using System;
using HEAppE.DataAccessTier.IRepository.ClusterInformation;
using HEAppE.DataAccessTier.IRepository.FileTransfer;
using HEAppE.DataAccessTier.IRepository.JobManagement;
using HEAppE.DataAccessTier.IRepository.JobManagement.Command;
using HEAppE.DataAccessTier.IRepository.JobManagement.JobInformation;
using HEAppE.DataAccessTier.IRepository.Notifications;
using HEAppE.DataAccessTier.IRepository.OpenStack;
using HEAppE.DataAccessTier.IRepository.UserAndLimitationManagement;

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
        IClusterProjectRepository ClusterProjectRepository { get; }
        IClusterProxyConnectionRepository ClusterProxyConnectionRepository { get; }
        IClusterNodeTypeRepository ClusterNodeTypeRepository { get; }
        IClusterNodeTypeRequestedGroupRepository ClusterNodeTypeRequestedGroupRepository { get; }
        IOpenStackAuthenticationCredentialsRepository OpenStackAuthenticationCredentialsRepository { get; }
        IOpenStackDomainRepository OpenStackDomainRepository { get; }
        IOpenStackInstanceRepository OpenStackInstanceRepository { get; }
        IOpenStackProjectRepository OpenStackProjectRepository { get; }
        IEnvironmentVariableRepository EnvironmentVariableRepository { get; }
        IFileTransferMethodRepository FileTransferMethodRepository { get; }
        IFileTransferTemporaryKeyRepository FileTransferTemporaryKeyRepository { get; }
        IFileSpecificationRepository FileSpecificationRepository { get; }
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
        ILanguageRepository LanguageRepository { get; }
        IMessageLocalizationRepository MessageLocalizationRepository { get; }
        IMessageTemplateRepository MessageTemplateRepository { get; }
        IMessageTemplateParameterRepository MessageTemplateParameterRepository { get; }
        INotificationRepository NotificationRepository { get; }
        IAdaptorUserRepository AdaptorUserRepository { get; }
        IAdaptorUserGroupRepository AdaptorUserGroupRepository { get; }
        IAdaptorUserRoleRepository AdaptorUserRoleRepository { get; }
        IResourceLimitationRepository ResourceLimitationRepository { get; }
        ISessionCodeRepository SessionCodeRepository { get; }
        IOpenStackSessionRepository OpenStackSessionRepository { get; }
        #endregion
        #region Methods
        void Save();
        #endregion
    }
}