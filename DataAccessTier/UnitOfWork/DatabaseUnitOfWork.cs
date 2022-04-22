using System;
using HEAppE.DataAccessTier.IRepository.ClusterInformation;
using HEAppE.DataAccessTier.IRepository.FileTransfer;
using HEAppE.DataAccessTier.IRepository.JobManagement;
using HEAppE.DataAccessTier.IRepository.JobManagement.Command;
using HEAppE.DataAccessTier.IRepository.JobManagement.JobInformation;
using HEAppE.DataAccessTier.IRepository.Notifications;
using HEAppE.DataAccessTier.IRepository.OpenStack;
using HEAppE.DataAccessTier.IRepository.UserAndLimitationManagement;
using HEAppE.DataAccessTier.Repository.ClusterInformation;
using HEAppE.DataAccessTier.Repository.FileTransfer;
using HEAppE.DataAccessTier.Repository.JobManagement;
using HEAppE.DataAccessTier.Repository.JobManagement.Command;
using HEAppE.DataAccessTier.Repository.JobManagement.JobInformation;
using HEAppE.DataAccessTier.Repository.Notifications;
using HEAppE.DataAccessTier.Repository.OpenStack;
using HEAppE.DataAccessTier.Repository.UserAndLimitationManagement;

namespace HEAppE.DataAccessTier.UnitOfWork
{
    public class DatabaseUnitOfWork : IUnitOfWork
    {
        #region Instances
        private readonly MiddlewareContext _context;
        private bool _disposed = false;

        private IAdaptorUserGroupRepository _adaptorUserGroupRepository;
        private IAdaptorUserRoleRepository _adaptorUserRoleRepository;
        private IAdaptorUserRepository _adaptorUserRepository;
        private IClusterProxyConnectionRepository _clusterProxyConnectionRepository;
        private IClusterRepository _clusterRepository;
        private IClusterAuthenticationCredentialsRepository _clusterAuthenticationCredentialsRepository;
        private IClusterNodeTypeRepository _clusterNodeTypeRepository;
        private IClusterNodeTypeRequestedGroupRepository _clusterNodeTypeRequestedRepository;
        private IOpenStackAuthenticationCredentialsRepository _openStackAuthenticationCredentialsRepository;
        private IOpenStackDomainRepository _openStackDomainRepository;
        private IOpenStackInstanceRepository _openStackInstanceRepository;
        private IOpenStackProjectDomainRepository _openStackProjectDomainRepository;
        private IOpenStackProjectRepository _openStackProjectRepository;
        private IEnvironmentVariableRepository _environmentVariableRepository;
        private IFileTransferMethodRepository _fileTransferMethodRepository;
        private IFileSpecificationRepository _fileSpecificationRepository;
        private ICommandTemplateRepository _commandTemplateRepository;
        private ICommandTemplateParameterRepository _commandTemplateParameterRepository;
        private ICommandTemplateParameterValueRepository _commandTemplateParameterValueRepository;
        private IJobSpecificationRepository _jobSpecificationRepository;
        private IJobTemplateRepository _jobTemplateRepository;
        private ITaskTemplateRepository _taskTemplateRepository;
        private ILanguageRepository _languageRepository;
        private IMessageLocalizationRepository _messagLocalizationRepository;
        private IMessageTemplateRepository _messageTemplateRepository;
        private IMessageTemplateParameterRepository _messageTemplateParameterRepository;
        private INotificationRepository _notificationRepository;
        private IPropertyChangeSpecificationRepository _propertyChangeSpecificationRepository;
        private IResourceLimitationRepository _resourceLimitationRepository;
        private ISessionCodeRepository _sessionCodeRepository;
        private ISubmittedJobInfoRepository _submittedJobInfoRepository;
        private ISubmittedTaskInfoRepository _submittedTaskInfoRepository;
        private ISubmittedTaskAllocationNodeInfoRepository _submittedTaskAllocationNodeInfoRepository;
        private ITaskSpecificationRepository _taskSpecificationRepository;
        private ITaskParalizationSpecificationRepository _taskParalizationSpecificationRepository;
        private ITaskSpecificationRequiredNodeRepository _taskSpecificationRequiredNodeRepository;
        private IOpenStackSessionRepository _openStackSessionRepository;
        #endregion
        #region Properties
        /// <summary>
        /// Check is disposed
        /// </summary>
        public bool IsDisposed => _disposed;
        #endregion
        #region Constructors
        public DatabaseUnitOfWork()
        {
            _context = new MiddlewareContext();
        }
        #endregion
        #region Repositories
        public IClusterProxyConnectionRepository ClusterProxyConnectionRepository
        {
            get
            {
                return _clusterProxyConnectionRepository = _clusterProxyConnectionRepository ?? new ClusterProxyConnectionRepository(_context);

            }
        }

        public IClusterRepository ClusterRepository
        {
            get
            {
                return _clusterRepository = _clusterRepository ?? new ClusterRepository(_context);

            }
        }

        public IClusterNodeTypeRepository ClusterNodeTypeRepository
        {
            get
            {

                return _clusterNodeTypeRepository = _clusterNodeTypeRepository ?? new ClusterNodeTypeRepository(_context);

            }
        }
        public IClusterNodeTypeRequestedGroupRepository ClusterNodeTypeRequestedGroupRepository
        {
            get
            {
                return _clusterNodeTypeRequestedRepository = _clusterNodeTypeRequestedRepository ?? new ClusterNodeTypeRequestedGroupRepository(_context);
            }
        }
        public IClusterAuthenticationCredentialsRepository ClusterAuthenticationCredentialsRepository
        {
            get
            {

                return _clusterAuthenticationCredentialsRepository = _clusterAuthenticationCredentialsRepository ?? new ClusterAuthenticationCredentialsRepository(_context);
            }
        }

        public IOpenStackAuthenticationCredentialsRepository OpenStackAuthenticationCredentialsRepository
        {
            get
            {

                return _openStackAuthenticationCredentialsRepository = _openStackAuthenticationCredentialsRepository ?? new OpenStackAuthenticationCredentialsRepository(_context);
            }
        }

        public IOpenStackDomainRepository OpenStackDomainRepository
        {
            get
            {
                return _openStackDomainRepository = _openStackDomainRepository ?? new OpenStackDomainRepository(_context);

            }
        }

        public IOpenStackInstanceRepository OpenStackInstanceRepository
        {
            get
            {
                return _openStackInstanceRepository = _openStackInstanceRepository ?? new OpenStackInstanceRepository(_context);

            }
        }

        public IOpenStackProjectDomainRepository OpenStackProjectDomainRepository
        {
            get
            {
                return _openStackProjectDomainRepository = _openStackProjectDomainRepository ?? new OpenStackProjectDomainRepository(_context);

            }
        }

        public IOpenStackProjectRepository OpenStackProjectRepository
        {
            get
            {
                return _openStackProjectRepository = _openStackProjectRepository ?? new OpenStackProjectRepository(_context);

            }
        }
 
        public IEnvironmentVariableRepository EnvironmentVariableRepository
        {
            get
            {
                return _environmentVariableRepository = _environmentVariableRepository ?? new EnvironmentVariableRepository(_context);
            }
        }

        public IFileTransferMethodRepository FileTransferMethodRepository
        {
            get
            {
                return _fileTransferMethodRepository = _fileTransferMethodRepository ?? new FileTransferMethodRepository(_context);
            }
        }

        public IFileSpecificationRepository FileSpecificationRepository
        {
            get
            {
                return _fileSpecificationRepository = _fileSpecificationRepository ?? new FileSpecificationRepository(_context);
            }
        }

        public ISubmittedJobInfoRepository SubmittedJobInfoRepository
        {
            get
            {
                return _submittedJobInfoRepository = _submittedJobInfoRepository ?? new SubmittedJobInfoRepository(_context);
            }
        }

        public ISubmittedTaskInfoRepository SubmittedTaskInfoRepository
        {
            get
            {
                return _submittedTaskInfoRepository = _submittedTaskInfoRepository ?? new SubmittedTaskInfoRepository(_context);
            }
        }
        public ISubmittedTaskAllocationNodeInfoRepository SubmittedTaskAllocationNodeInfoRepository
        {
            get
            {
                return _submittedTaskAllocationNodeInfoRepository = _submittedTaskAllocationNodeInfoRepository ?? new SubmittedTaskAllocationNodeInfoRepository(_context);
            }
        }


        public ICommandTemplateRepository CommandTemplateRepository
        {
            get
            {
                return _commandTemplateRepository = _commandTemplateRepository ?? new CommandTemplateRepository(_context);
            }
        }

        public ICommandTemplateParameterRepository CommandTemplateParameterRepository
        {
            get
            {
                return _commandTemplateParameterRepository = _commandTemplateParameterRepository ?? new CommandTemplateParameterRepository(_context);
            }
        }

        public ICommandTemplateParameterValueRepository CommandTemplateParameterValueRepository
        {
            get
            {
                return _commandTemplateParameterValueRepository = _commandTemplateParameterValueRepository ?? new CommandTemplateParameterValueRepository(_context);
            }
        }

        public IJobSpecificationRepository JobSpecificationRepository
        {
            get
            {
                return _jobSpecificationRepository = _jobSpecificationRepository ?? new JobSpecificationRepository(_context);
            }
        }

        public IJobTemplateRepository JobTemplateRepository
        {
            get
            {
                return _jobTemplateRepository = _jobTemplateRepository ?? new JobTemplateRepository(_context);
            }
        }

        public ITaskTemplateRepository TaskTemplateRepository
        {
            get
            {
                return _taskTemplateRepository = _taskTemplateRepository ?? new TaskTemplateRepository(_context);
            }
        }

        public ITaskSpecificationRepository TaskSpecificationRepository
        {
            get
            {
                return _taskSpecificationRepository = _taskSpecificationRepository ?? new TaskSpecificationRepository(_context);
            }
        }
        public ITaskParalizationSpecificationRepository TaskParalizationSpecificationRepository
        {
            get
            {
                return _taskParalizationSpecificationRepository = _taskParalizationSpecificationRepository ?? new TaskParalizationSpecificationRepository(_context);
            }
        }

        public ITaskSpecificationRequiredNodeRepository TaskSpecificationRequiredNodeRepository
        {
            get
            {
                return _taskSpecificationRequiredNodeRepository = _taskSpecificationRequiredNodeRepository ?? new TaskSpecificationRequiredNodeRepository(_context);
            }
        }

        public ILanguageRepository LanguageRepository
        {
            get
            {
                return _languageRepository = _languageRepository ?? new LanguageRepository(_context);
            }
        }

        public IMessageLocalizationRepository MessageLocalizationRepository
        {
            get
            {
                return _messagLocalizationRepository = _messagLocalizationRepository ?? new MessageLocalizationRepository(_context);
            }
        }

        public IMessageTemplateRepository MessageTemplateRepository
        {
            get
            {
                return _messageTemplateRepository = _messageTemplateRepository ?? new MessageTemplateRepository(_context);
            }
        }

        public IMessageTemplateParameterRepository MessageTemplateParameterRepository
        {
            get
            {
                return _messageTemplateParameterRepository = _messageTemplateParameterRepository ?? new MessageTemplateParameterRepository(_context);
            }
        }

        public INotificationRepository NotificationRepository
        {
            get
            {
                return _notificationRepository = _notificationRepository ?? new NotificationRepository(_context);
            }
        }

        public IPropertyChangeSpecificationRepository PropertyChangeSpecificationRepository
        {
            get
            {
                return _propertyChangeSpecificationRepository = _propertyChangeSpecificationRepository ?? new PropertyChangeSpecificationRepository(_context);
            }
        }

        public IAdaptorUserRepository AdaptorUserRepository
        {
            get
            {
                return _adaptorUserRepository = _adaptorUserRepository ?? new AdaptorUserRepository(_context);
            }
        }

        public IAdaptorUserGroupRepository AdaptorUserGroupRepository
        {
            get
            {
                return _adaptorUserGroupRepository = _adaptorUserGroupRepository ?? new AdaptorUserGroupRepository(_context);
            }
        }

        public IAdaptorUserRoleRepository AdaptorUserRoleRepository
        {
            get
            {
                return _adaptorUserRoleRepository = _adaptorUserRoleRepository ?? new AdaptorUserRoleRepository(_context);
            }
        }

        public IResourceLimitationRepository ResourceLimitationRepository
        {
            get
            {
                return _resourceLimitationRepository = _resourceLimitationRepository ?? new ResourceLimitationRepository(_context);
            }
        }

        public ISessionCodeRepository SessionCodeRepository
        {
            get
            {
                return _sessionCodeRepository = _sessionCodeRepository ?? new SessionCodeRepository(_context);
            }
        }

        public IOpenStackSessionRepository OpenStackSessionRepository
        {
            get
            {
                return _openStackSessionRepository = _openStackSessionRepository ?? new OpenStackSessionRepository(_context);
            }
        }
        #endregion
        #region Methods
        /// <summary>
        /// Save
        /// </summary>
        public void Save()
        {
            _context.SaveChanges();
        }
        #endregion
        #region IDisposable Methods
        /// <summary>
        /// Dispose
        /// </summary>
        /// <param name="disposing">IS disposing</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _context.Dispose();
                }
            }
            _disposed = true;
        }

        /// <summary>
        /// Dispose
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}