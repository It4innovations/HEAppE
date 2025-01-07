using System;
using System.Linq;
using HEAppE.DataAccessTier.IRepository.ClusterInformation;
using HEAppE.DataAccessTier.IRepository.FileTransfer;
using HEAppE.DataAccessTier.IRepository.JobManagement;
using HEAppE.DataAccessTier.IRepository.JobManagement.Command;
using HEAppE.DataAccessTier.IRepository.JobManagement.JobInformation;
using HEAppE.DataAccessTier.IRepository.OpenStack;
using HEAppE.DataAccessTier.IRepository.UserAndLimitationManagement;
using HEAppE.DataAccessTier.Repository.ClusterInformation;
using HEAppE.DataAccessTier.Repository.FileTransfer;
using HEAppE.DataAccessTier.Repository.JobManagement;
using HEAppE.DataAccessTier.Repository.JobManagement.Command;
using HEAppE.DataAccessTier.Repository.JobManagement.JobInformation;
using HEAppE.DataAccessTier.Repository.OpenStack;
using HEAppE.DataAccessTier.Repository.UserAndLimitationManagement;
using HEAppE.DataAccessTier.Vault;
using HEAppE.DomainObjects.ClusterInformation;

namespace HEAppE.DataAccessTier.UnitOfWork;

public class DatabaseUnitOfWork : IUnitOfWork
{
    #region Constructors

    public DatabaseUnitOfWork()
    {
        _context = new MiddlewareContext();
    }

    #endregion

    #region Properties

    /// <summary>
    ///     Check is disposed
    /// </summary>
    public bool IsDisposed { get; private set; }

    #endregion

    #region Methods

    /// <summary>
    ///     Save
    /// </summary>
    public void Save()
    {
        // prepare vault entities
        var vaultEntries = _context.ChangeTracker.Entries<ClusterAuthenticationCredentials>()
            .Select(x => x.Entity)
            .ToList();

        var changes = _context.SaveChanges();

        //SavePreparedEntitiesToVault(changes); //JK - commented out to avoid vault access

        /*
        void SavePreparedEntitiesToVault(int dbChanges)
        {
            if ((dbChanges < vaultEntries.Count))
            {
                // TODO logging
            }
            foreach (var ve in vaultEntries)
            {
                // if private key is empty, try to get it from vault to be sure that it is relevant and not some relation access issue
                if (string.IsNullOrEmpty(ve.PrivateKey))
                {
                    var vaultData = _vaultConnector.GetClusterAuthenticationCredentials(ve.Id).GetAwaiter().GetResult();
                    if (vaultData != null && vaultData.PrivateKey != ve.PrivateKey)
                    {
                        ve.ImportVaultData(vaultData);
                    }
                }
                _vaultConnector.SetClusterAuthenticationCredentials(ve.ExportVaultData());
            }
        }
        */
    }

    #endregion

    #region Instances

    private readonly MiddlewareContext _context;

    private IVaultConnector _vaultConnector { get; } = new VaultConnector();

    private IAdaptorUserGroupRepository _adaptorUserGroupRepository;
    private IAdaptorUserRoleRepository _adaptorUserRoleRepository;
    private IAdaptorUserRepository _adaptorUserRepository;
    private IClusterProxyConnectionRepository _clusterProxyConnectionRepository;
    private IClusterRepository _clusterRepository;
    private IProjectRepository _projectRepository;
    private ISubProjectRepository _subProjectRepository;
    private IContactRepository _contactRepository;
    private IClusterProjectRepository _clusterProjectRepository;
    private IClusterAuthenticationCredentialsRepository _clusterAuthenticationCredentialsRepository;
    private IClusterNodeTypeRepository _clusterNodeTypeRepository;
    private IClusterNodeTypeRequestedGroupRepository _clusterNodeTypeRequestedRepository;
    private IClusterNodeTypeAggregationRepository _clusterNodeTypeAggregationRepository;
    private IClusterNodeTypeAggregationAccountingRepository _clusterNodeTypeAggregationAccountingRepository;
    private IProjectClusterNodeTypeAggregationRepository _projectClusterNodeTypeAggregationRepository;
    private IOpenStackAuthenticationCredentialsRepository _openStackAuthenticationCredentialsRepository;
    private IOpenStackDomainRepository _openStackDomainRepository;
    private IOpenStackInstanceRepository _openStackInstanceRepository;
    private IOpenStackProjectRepository _openStackProjectRepository;
    private IEnvironmentVariableRepository _environmentVariableRepository;
    private IFileTransferMethodRepository _fileTransferMethodRepository;
    private IFileTransferTemporaryKeyRepository _fileTransferTemporaryKeyRepository;
    private IFileSpecificationRepository _fileSpecificationRepository;
    private ICommandTemplateRepository _commandTemplateRepository;
    private ICommandTemplateParameterRepository _commandTemplateParameterRepository;
    private ICommandTemplateParameterValueRepository _commandTemplateParameterValueRepository;
    private IJobSpecificationRepository _jobSpecificationRepository;
    private ISessionCodeRepository _sessionCodeRepository;
    private IAccountingRepository _accountingRepository;
    private ISubmittedJobInfoRepository _submittedJobInfoRepository;
    private ISubmittedTaskInfoRepository _submittedTaskInfoRepository;
    private ISubmittedTaskAllocationNodeInfoRepository _submittedTaskAllocationNodeInfoRepository;
    private ITaskSpecificationRepository _taskSpecificationRepository;
    private ITaskParalizationSpecificationRepository _taskParalizationSpecificationRepository;
    private ITaskSpecificationRequiredNodeRepository _taskSpecificationRequiredNodeRepository;
    private IOpenStackSessionRepository _openStackSessionRepository;

    #endregion

    #region Repositories

    public IClusterProxyConnectionRepository ClusterProxyConnectionRepository
    {
        get
        {
            return _clusterProxyConnectionRepository =
                _clusterProxyConnectionRepository ?? new ClusterProxyConnectionRepository(_context);
        }
    }

    public IClusterRepository ClusterRepository
    {
        get { return _clusterRepository = _clusterRepository ?? new ClusterRepository(_context); }
    }

    public IProjectRepository ProjectRepository
    {
        get { return _projectRepository = _projectRepository ?? new ProjectRepository(_context); }
    }

    public ISubProjectRepository SubProjectRepository
    {
        get { return _subProjectRepository = _subProjectRepository ?? new SubProjectRepository(_context); }
    }

    public IContactRepository ContactRepository
    {
        get { return _contactRepository = _contactRepository ?? new ContactRepository(_context); }
    }

    public IClusterProjectRepository ClusterProjectRepository
    {
        get { return _clusterProjectRepository = _clusterProjectRepository ?? new ClusterProjectRepository(_context); }
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
            return _clusterNodeTypeRequestedRepository = _clusterNodeTypeRequestedRepository ??
                                                         new ClusterNodeTypeRequestedGroupRepository(_context);
        }
    }

    public IClusterNodeTypeAggregationRepository ClusterNodeTypeAggregationRepository
    {
        get
        {
            return _clusterNodeTypeAggregationRepository = _clusterNodeTypeAggregationRepository ??
                                                           new ClusterNodeTypeAggregationRepository(_context);
        }
    }

    public IClusterNodeTypeAggregationAccountingRepository ClusterNodeTypeAggregationAccountingRepository
    {
        get
        {
            return _clusterNodeTypeAggregationAccountingRepository = _clusterNodeTypeAggregationAccountingRepository ??
                                                                     new ClusterNodeTypeAggregationAccountingRepository(
                                                                         _context);
        }
    }

    public IProjectClusterNodeTypeAggregationRepository ProjectClusterNodeTypeAggregationRepository
    {
        get
        {
            return _projectClusterNodeTypeAggregationRepository = _projectClusterNodeTypeAggregationRepository ??
                                                                  new ProjectClusterNodeTypeAggregationRepository(
                                                                      _context);
        }
    }

    public IClusterAuthenticationCredentialsRepository ClusterAuthenticationCredentialsRepository
    {
        get
        {
            return _clusterAuthenticationCredentialsRepository = _clusterAuthenticationCredentialsRepository
                                                                 ?? new ClusterAuthenticationCredentialsRepository(
                                                                     _context, _vaultConnector);
        }
    }

    public IOpenStackAuthenticationCredentialsRepository OpenStackAuthenticationCredentialsRepository
    {
        get
        {
            return _openStackAuthenticationCredentialsRepository = _openStackAuthenticationCredentialsRepository ??
                                                                   new OpenStackAuthenticationCredentialsRepository(
                                                                       _context);
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
            return _openStackInstanceRepository =
                _openStackInstanceRepository ?? new OpenStackInstanceRepository(_context);
        }
    }

    public IOpenStackProjectRepository OpenStackProjectRepository
    {
        get
        {
            return _openStackProjectRepository =
                _openStackProjectRepository ?? new OpenStackProjectRepository(_context);
        }
    }

    public IEnvironmentVariableRepository EnvironmentVariableRepository
    {
        get
        {
            return _environmentVariableRepository =
                _environmentVariableRepository ?? new EnvironmentVariableRepository(_context);
        }
    }

    public IFileTransferMethodRepository FileTransferMethodRepository
    {
        get
        {
            return _fileTransferMethodRepository =
                _fileTransferMethodRepository ?? new FileTransferMethodRepository(_context);
        }
    }

    public IFileTransferTemporaryKeyRepository FileTransferTemporaryKeyRepository
    {
        get
        {
            return _fileTransferTemporaryKeyRepository = _fileTransferTemporaryKeyRepository ??
                                                         new FileTransferTemporaryKeyRepository(_context);
        }
    }

    public IFileSpecificationRepository FileSpecificationRepository
    {
        get
        {
            return _fileSpecificationRepository =
                _fileSpecificationRepository ?? new FileSpecificationRepository(_context);
        }
    }

    public IAccountingRepository AccountingRepository
    {
        get { return _accountingRepository = _accountingRepository ?? new AccountingRepository(_context); }
    }

    public ISubmittedJobInfoRepository SubmittedJobInfoRepository
    {
        get
        {
            return _submittedJobInfoRepository =
                _submittedJobInfoRepository ?? new SubmittedJobInfoRepository(_context);
        }
    }

    public ISubmittedTaskInfoRepository SubmittedTaskInfoRepository
    {
        get
        {
            return _submittedTaskInfoRepository =
                _submittedTaskInfoRepository ?? new SubmittedTaskInfoRepository(_context);
        }
    }

    public ISubmittedTaskAllocationNodeInfoRepository SubmittedTaskAllocationNodeInfoRepository
    {
        get
        {
            return _submittedTaskAllocationNodeInfoRepository = _submittedTaskAllocationNodeInfoRepository ??
                                                                new SubmittedTaskAllocationNodeInfoRepository(_context);
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
            return _commandTemplateParameterRepository = _commandTemplateParameterRepository ??
                                                         new CommandTemplateParameterRepository(_context);
        }
    }

    public ICommandTemplateParameterValueRepository CommandTemplateParameterValueRepository
    {
        get
        {
            return _commandTemplateParameterValueRepository = _commandTemplateParameterValueRepository ??
                                                              new CommandTemplateParameterValueRepository(_context);
        }
    }

    public IJobSpecificationRepository JobSpecificationRepository
    {
        get
        {
            return _jobSpecificationRepository =
                _jobSpecificationRepository ?? new JobSpecificationRepository(_context);
        }
    }

    public ITaskSpecificationRepository TaskSpecificationRepository
    {
        get
        {
            return _taskSpecificationRepository =
                _taskSpecificationRepository ?? new TaskSpecificationRepository(_context);
        }
    }

    public ITaskParalizationSpecificationRepository TaskParalizationSpecificationRepository
    {
        get
        {
            return _taskParalizationSpecificationRepository = _taskParalizationSpecificationRepository ??
                                                              new TaskParalizationSpecificationRepository(_context);
        }
    }

    public ITaskSpecificationRequiredNodeRepository TaskSpecificationRequiredNodeRepository
    {
        get
        {
            return _taskSpecificationRequiredNodeRepository = _taskSpecificationRequiredNodeRepository ??
                                                              new TaskSpecificationRequiredNodeRepository(_context);
        }
    }

    public IAdaptorUserRepository AdaptorUserRepository
    {
        get { return _adaptorUserRepository = _adaptorUserRepository ?? new AdaptorUserRepository(_context); }
    }

    public IAdaptorUserGroupRepository AdaptorUserGroupRepository
    {
        get
        {
            return _adaptorUserGroupRepository =
                _adaptorUserGroupRepository ?? new AdaptorUserGroupRepository(_context);
        }
    }

    public IAdaptorUserRoleRepository AdaptorUserRoleRepository
    {
        get
        {
            return _adaptorUserRoleRepository = _adaptorUserRoleRepository ?? new AdaptorUserRoleRepository(_context);
        }
    }

    public ISessionCodeRepository SessionCodeRepository
    {
        get { return _sessionCodeRepository = _sessionCodeRepository ?? new SessionCodeRepository(_context); }
    }

    public IOpenStackSessionRepository OpenStackSessionRepository
    {
        get
        {
            return _openStackSessionRepository =
                _openStackSessionRepository ?? new OpenStackSessionRepository(_context);
        }
    }

    #endregion

    #region IDisposable Methods

    /// <summary>
    ///     Dispose
    /// </summary>
    /// <param name="disposing">IS disposing</param>
    protected virtual void Dispose(bool disposing)
    {
        if (!IsDisposed)
            if (disposing)
                _context.Dispose();
        IsDisposed = true;
    }

    /// <summary>
    ///     Dispose
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    #endregion
}