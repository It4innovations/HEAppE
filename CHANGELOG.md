# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## V6.2.3

### Added
- **User-specific tunnel reuse**: Implemented logic to identify and reuse existing active tunnels for the same user and task. This prevents redundant SSH connection overhead and optimizes local port utilization.
- **Resilient Database Restore:** Optimized the restoration workflow by pre-fetching backup metadata, implementing atomic state switching to prevent connection hijacking, and adding automatic recovery failsafes to eliminate the risk of databases remaining in a Restoring state.
- Added logic for invalidation all caches with required admin role.

### Changed
- UserOrg Command Template authorization service now grants access if the template is enabled in at least one matching project resource entry (if enabled).

## V6.2.2

### Changed
- Increased accounting string attribute length in the `Project` object to 150 characters.
- Enhanced HPC connection robustness to Retry and timeout logic when Initializing connection via `SSH`, `SFPT` client.
- Extended length of string atrributes of `CommandTemplate` to 1000 characters.
- Optimised data transfer and tunnel management.
- Propagated `SSH Client` error to HEAppE API responses. 


## V6.2.1

### Added
- **Robust Role Validation**: Added `HashSet`-based deduplication and enhanced navigation property checks to ensure data integrity during system role synchronization.
- Extended `TestClusterAccessForAccount` logging to handle error states more efectively.
- Increased string length for selected attributes of `Cluster`, `ClusterNodeType` and `AdaptorUser` domain object properties to 250.


### Fixed
- **EF Change Tracker Sync**: Resolved issues where the role assignment logic would fail to recognize recently added entities in the same transaction.
- Fixed error in automatic procedure for the cluster script initialization on the cluster side.
- Removed external network configuration from the docker compose to allow run multiple HEAppE instances at one VM.
- Fixed UserRole Assignment for User which created new Project.

## V6.2.0

### Added
- Added dry-run Slurm job submission endpoint `POST /heappe/JobManagement/DryRunJob` to simulate scheduling without execution, returning predicted start time and resource allocation.
- Added GPU count calculation in `SlurmTaskAdapter` for partial resource allocation.
- Added an automated procedure to monitor updates to HEAppE key scripts.
  - Implemented automatic sync procedure to the cluster user space upon detection of changes.
- New Management endpoints for file uploads:
  - `/api/DataStaging/UploadFilesToProjectDir` – Upload files to project directory (Manager role, DataStagingAPI).
  - `/api/DataStaging/UploadJobScriptsToProjectDir` – Upload job scripts to project directory and make them executable (Manager role, DataStagingAPI).
  - `/heappe/FileTransfer/UploadFilesToJobExecutionDir` – Upload files to job execution directory and optinally directly into task directory (Submitter role, RestAPI).
- Added IQueryable-based user-specific job retrieval logic for more efficient filtering.
- Added global cache invalidation mechanism with enhanced cache entry management for `ListAvalialbleClusters` endpoint.
- Added `SubmittedJobInfoId` to `GetDataTransferMethodModel` to enhance task info handling.
- Support for checking permissions of the single user to use `Command Template` by the `User Org Service`.
- Implemented automatic and on-trigger backup systems for the HEAppE vault and configuration files.
- Introduced new API endpoints for:
    - `AdaptorUsers` management.
    - `Project Role` assignment and unassignment.
    - `User Group Role` assignment and unassignment.
- Added HEAppE Admin Roles for Management and Reporting.
- Added support for `X-API-Key` header authentication, allowing full system operation without requiring `SessionCodes`.
- Added `Expirio service` adapter.
    - Implemented token exchange functionality.


### Changed
- Enhanced cluster authentication logic with improved error handling.
- Enhanced cluster listing and caching with user validation and improved filtering.
- Enhanced file listing in `SftpFileSystemManager` with better relative path handling.
- Improved exception handling for unauthorized access.
- Introduced `.part` temporary upload extension with rename after upload completion.
- Renamed `PermanentStoragePath` to `ProjectStoragePath`.
- Asynchronous Processing: extensive implementation of async/await methods across Repositories and Services for non-blocking I/O operations.
- Applied `AsNoTracking` in `JobManagementService` for read-only queries to reduce change tracker overhead.
- Utilized `AsSplitQuery` in `ClusterAuthenticationCredentialsRepository` to resolve possible `Cartesian explosion` issues during complex joins.
- Refined filtering logic for user-specific job retrieval.
- Rewrote ConnectionPool using ConcurrentDictionary and SemaphoreSlim to ensure thread safety and prevent race conditions under load.
- Optimized pooling by introducing user-specific slots.
- Updated VaultConnector to use a singleton HttpClient instance to prevent socket exhaustion.
- Implemented thread-safe caching for `Vault` data to minimize external API calls.
- Enhanced `ClusterProjectCredentialVaultPart` with null-safe JSON processing, robust serialization, and improved error handling.
- Optimized the job specification completion process and task processing logic.
- Optimized `Service Registration` logic.
- Consolidated `Lexis Token Service`.
- Improved retrieval efficiency for `AdaptorUser` and `SessionCode` entities.
- Decoupled the startup procedure from background job initialization, preventing background tasks from blocking the system boot process.
- Made `/heappe/Health` endpoint publicly accessible for all deployments.

## V6.1.1

### Fixed
- Incorrect handling of Slurm status updates for array jobs submitted paralelly under the same account. This caused tasks to be incorrectly marked as failed despite the underlying Slurm jobs being PENDING, RUNNING, or COMPLETED.

## V6.1.0

### Added
- Bearer authentication for LEXIS – implemented support for JWT bearer tokens and introspection for federated identity providers.
- SSH CA support – added API endpoints and authentication mechanism for SSH Certificate Authority (SSH CA) access on clusters.
- Management automation enhancements:
  - Background worker now automatically performs database backups according to configured schedule.
  - Automatic detection and tracking of stored account statuses.
  - Status checks are performed via dry-run jobs for verification without execution.
- New Management endpoints for database maintenance:
  - `GET /heappe/Management/Backups` – List database backups
  - `POST /heappe/Management/BackupDatabase` – Perform full database backup
  - `POST /heappe/Management/BackupDatabaseTransactionLogs` – Backup database transaction logs
  - `POST /heappe/Management/RestoreDatabase` – Restore database from a specified backup file
- DataTransfer endpoint for large payloads:
  - `POST /heappe/DataTransfer/HttpPostToJobNodeStream` – Stream large HTTP POST requests to the job node, designed for AI inference use cases.

### Changed
- Updated Swagger documentation to include new Management endpoints.
- Extended internal ManagementService with backup and restore functionalities.

### Fixed
- Minor stability and logging improvements.

### New Endpoints: 7
--------------------
POST /heappe/DataTransfer/HttpPostToJobNodeStream  
POST /heappe/Management/BackupDatabase  
POST /heappe/Management/BackupDatabaseTransactionLogs  
GET /heappe/Management/Backups  
POST /heappe/Management/RestoreDatabase  
POST /heappe/Management/Status  
POST /heappe/Management/StatusErrorLogs  

### Deleted Endpoints: 4
------------------------
DELETE /heappe/Management/SecureShellKey  
POST /heappe/Management/SecureShellKey  
PUT /heappe/Management/SecureShellKey  
POST /heappe/Management/TestClusterAccessForAccount  

### Modified Endpoints: 3
-------------------------
GET /heappe/ClusterInformation/ListAvailableClusters
- Modified query param: SessionCode
  - Required changed from true to false

POST /heappe/FileTransfer/RequestFileTransfer
- Responses changed
  - Modified response: 200
    - Content changed
      - Modified media type: application/json
        - Schema changed
          - Properties changed
            - Modified property: Credentials
              - Properties changed
                - Modified property: CredentialsAuthType
                  - New enum values: [10 11]

## V6.0.0

### Changed
- Enhanced logging in `DataTransfer` endpoints
- Redefined `Job Execution` and `Job Log Archive path` (dedicated to specific HEAppE instance and cluster user)
- Redefined path to `cluster scripts` and setup for each `ClusterAuthenticationCredential` and `Project`
- `Management/InitializeClusterScriptDirectory` body and business logic
- Response structure of the `TestClusterAccessForAccount` (added info/check about specific access to the cluster)
- Unify Attribute Names in CopyJobDataToTemp and CopyJobDataFromTemp endpoint payloads
- Optimized `ListAvailableClusters` endpoint and enhanced caching

### Added
- 1:1 user mapping to SSH key at `Project` level
- Configurable authentization method by JWT Token introspection and validation
- Possibility to send `application/json` payload with `HttpPostToJobNode`
- Support for the `EdDSA - ED25519` SSH key pair generation
- Options `ConnectionRetryAttempts` and `ConnectionTimeout` for SSH client component are now configurable from `appsettings.json`
- API HTTP Request logging with payload (redacted output on `Sensitive data`)
- Add `/heappe/Health` check endpoint
- Health checks are not visible in swagger
- `Reason` attribute propagation from the HPC job (in the HEAppE Task)
- `IsInitialized` attribute for `ClusterAuthenticationCredentials` with check for all endpoints which uses `ClusterAuthenticationCredential` to connect HPC
- Endpoints for bulk listing of `ClusterNodeTypes`, `FileTransferMethods`, `Projects`, `ClusterNodeTypeAggregationAccountings`
- Endpoint to Reset ListAvailableClusters Memory Cache
- Automatic cluster account initialization feature configurable from `appsettings.json` 

### Fixed
- `External UsageType` model conversion to `Internal UsageType` model (enum)
- Fixed wrong error messages for CommandTemplateParameters methods in ManagementService
- Typo in TaskParallelizationParameters in the `HEAppE Task` specificaton in `CreateJob` endpoint
- Corrected the `SubmittedJobInfoId` field to `SubmittedTaskInfoId` in REST API endpoints `heappe/DataTransfer/RequestDataTransfer`, `heappe/DataTransfer/HttpPostToJobNode`, and `heappe/DataTransfer/HttpGetToJobNode` to reflect that these endpoints operate on Submitted Task according to the service and business logic tier
- Implemented logic to automatically split SSH command requests to remove SSH keys in the Background Worker when exceeding the maximum SSH.NET packet size, ensuring complete removal of temporary keys without encountering the error.
- Multiple SSH Tunnel creation support and port allocation when using `heappe/DataTransfer/RequestDataTransfer` endpoint
- SessionCode regeneration method


### New Endpoints: 11
---------------------
POST /heappe/ClusterInformation/ListAvailableClustersClearCache  
GET /heappe/Health  
GET /heappe/Management/Accountings  
GET /heappe/Management/ClusterAccountStatus  
GET /heappe/Management/ClusterNodeTypeAggregationAccountings  
GET /heappe/Management/ClusterNodeTypes  
GET /heappe/Management/ClusterProxyConnections  
GET /heappe/Management/FileTransferMethods  
PUT /heappe/Management/ModifyClusterAuthenticationCredential  
GET /heappe/Management/ProjectAssignmentToClusters  
GET /heappe/Management/Projects  

### Deleted Endpoints: None
---------------------------

### Modified Endpoints: 17
--------------------------
GET /heappe/ClusterInformation/ListAvailableClusters
- New query param: ForceRefresh

POST /heappe/DataTransfer/HttpGetToJobNode

POST /heappe/DataTransfer/HttpPostToJobNode

POST /heappe/DataTransfer/RequestDataTransfer

POST /heappe/JobManagement/CopyJobDataToTemp

POST /heappe/JobManagement/CreateJob

GET /heappe/JobReporting/JobsDetailedReport
- New query param: TimeFrom
- New query param: TimeTo
- Modified query param: SessionCode
  - Description changed from 'Session code' to ''
- Modified query param: SubProjects
  - Description changed from 'SubProjects' to ''

DELETE /heappe/Management/CommandTemplateParameter
- Summary changed from 'Remove Static Command Template' to 'Remove an existing Command Template Parameter'

GET /heappe/Management/CommandTemplateParameter
- Summary changed from 'Get CommandTemplateParameter by id' to 'Get Command Template Parameter by id'

POST /heappe/Management/CommandTemplateParameter
- Summary changed from 'Create Static Command Template' to 'Create a new Command Template Parameter'

PUT /heappe/Management/CommandTemplateParameter
- Summary changed from 'Modify Static Command Template' to 'Modify an existing Command Template Parameter'

POST /heappe/Management/InitializeClusterScriptDirectory

POST /heappe/Management/Project

PUT /heappe/Management/Project

POST /heappe/Management/ProjectAssignmentToCluster

PUT /heappe/Management/ProjectAssignmentToCluster

GET /heappe/Management/TestClusterAccessForAccount
- Responses changed
  - Modified response: 200
    - Extensions changed
      - Modified extension: schema
        - Added /items with value: 'map[$ref:#/definitions/ClusterAccessReportExt]'
        - Modified /type from 'string' to 'array'

Other Changes
-------------
Extensions changed
- Modified extension: definitions
  - Added /ClusterAccessReportExt with value: 'map[additionalProperties:false properties:map[ClusterName:map[description:Cluster name type:string] IsClusterAccessible:map[description:Is cluster accessible type:boolean]] type:object]'
  - Added /ClusterAccountStatusExt with value: 'map[additionalProperties:false properties:map[Cluster:map[$ref:#/definitions/ClusterExt] IsInitialized:map[description:Is initialized type:boolean] Project:map[$ref:#/definitions/ProjectExt]] type:object]'
  - Added /ClusterExt/properties/FileTransferMethodIds with value: 'map[description:File transfer ids items:map[format:int64 type:integer] type:array]'
  - Added /ClusterNodeTypeExt/properties/ClusterId with value: 'map[description:Cluster id format:int64 type:integer]'
  - Removed /ClusterProjectExt/properties/LocalBasepath with value: 'map[description:Local base path maxLength:100 minLength:0 type:string]'
  - Added /ClusterProjectExt/properties/PermanentStoragePath with value: 'map[description:Permanent Storage Path maxLength:100 minLength:0 type:string]'
  - Added /ClusterProjectExt/properties/ScratchStoragePath with value: 'map[description:Scratch Storage Path maxLength:100 minLength:0 type:string]'
  - Modified /CommandTemplateParameterValueExt/properties/ParameterValue/maxLength from '1000' to '100000'
  - Added /CopyJobDataToTempModel/properties/CreatedJobInfoId with value: 'map[description:Created job info id format:int64 type:integer]'
  - Removed /CopyJobDataToTempModel/properties/SubmittedJobInfoId with value: 'map[description:Subbmited job info id format:int64 type:integer]'
  - Removed /CreateProjectAssignmentToClusterModel/properties/LocalBasepath with value: 'map[description:Local base path maxLength:100 minLength:0 type:string]'
  - Added /CreateProjectAssignmentToClusterModel/properties/PermanentStoragePath with value: 'map[description:Permanent Storage Path maxLength:100 minLength:0 type:string]'
  - Added /CreateProjectAssignmentToClusterModel/properties/ScratchStoragePath with value: 'map[description:Scratch Storage Path maxLength:100 minLength:0 type:string]'
  - Added /CreateProjectModel/properties/IsOneToOneMapping with value: 'map[description:Map user account to exact robot account type:boolean]'
  - Added /Database_ with value: 'map[additionalProperties:false properties:map[IsHealthy:map[description:Database is healthy type:boolean]] type:object]'
  - Added /ExtendedClusterExt/properties/FileTransferMethodIds with value: 'map[description:File transfer ids items:map[format:int64 type:integer] type:array]'
  - Modified /FileTransferCipherTypeExt/enum/0 from '0' to '1'
  - Modified /FileTransferCipherTypeExt/enum/1 from '1' to '2'
  - Modified /FileTransferCipherTypeExt/enum/2 from '2' to '3'
  - Modified /FileTransferCipherTypeExt/enum/3 from '3' to '4'
  - Modified /FileTransferCipherTypeExt/enum/4 from '4' to '5'
  - Modified /FileTransferCipherTypeExt/enum/5 from '5' to '6'
  - Added /FileTransferMethodNoCredentialsExt/properties/ClusterId with value: 'map[description:Cluster id format:int64 type:integer]'
  - Removed /GetDataTransferMethodModel/properties/SubmittedJobInfoId with value: 'map[description:Subbmited job info id format:int64 type:integer]'
  - Added /GetDataTransferMethodModel/properties/SubmittedTaskInfoId with value: 'map[description:Submitted task info id format:int64 type:integer]'
  - Added /HealthComponent_ with value: 'map[additionalProperties:false properties:map[Database:map[$ref:#/definitions/Database_] Vault:map[$ref:#/definitions/Vault_]] type:object]'
  - Added /HealthExt with value: 'map[additionalProperties:false properties:map[Component:map[$ref:#/definitions/HealthComponent_] IsHealthy:map[description:IsHealthy type:boolean] Timestamp:map[description:Timestamp format:date-time type:string] Version:map[description:Version type:string]] type:object]'
  - Removed /HttpGetToJobNodeModel/properties/SubmittedJobInfoId with value: 'map[description:Subbmited job info id format:int64 type:integer]'
  - Added /HttpGetToJobNodeModel/properties/SubmittedTaskInfoId with value: 'map[description:Submitted task info id format:int64 type:integer]'
  - Removed /HttpPostToJobNodeModel/properties/SubmittedJobInfoId with value: 'map[description:Subbmited job info id format:int64 type:integer]'
  - Added /HttpPostToJobNodeModel/properties/SubmittedTaskInfoId with value: 'map[description:Submitted task info id format:int64 type:integer]'
  - Removed /InitializeClusterScriptDirectoryModel/properties/ClusterProjectRootDirectory with value: 'map[description:Cluster project root directory type:string]'
  - Added /InitializeClusterScriptDirectoryModel/properties/OverwriteExistingProjectRootDirectory with value: 'map[description:Overwrite existing cluster project root directory type:boolean]'
  - Added /InitializeClusterScriptDirectoryModel/properties/Username with value: 'map[description:Username type:string]'
  - Added /ModifyClusterAuthenticationCredentialModel with value: 'map[additionalProperties:false properties:map[NewPassword:map[description:New password type:string] NewUsername:map[description:New username type:string] OldUsername:map[description:Old username type:string] ProjectId:map[description:Project ID format:int64 type:integer] SessionCode:map[description:Session code maxLength:50 minLength:0 type:string]] type:object]'
  - Removed /ModifyProjectAssignmentToClusterModel/properties/LocalBasepath with value: 'map[description:Local base path maxLength:100 minLength:0 type:string]'
  - Added /ModifyProjectAssignmentToClusterModel/properties/PermanentStoragePath with value: 'map[description:Permanent Storage Path maxLength:100 minLength:0 type:string]'
  - Added /ModifyProjectAssignmentToClusterModel/properties/ScratchStoragePath with value: 'map[description:Scratch Storage Path maxLength:100 minLength:0 type:string]'
  - Added /ModifyProjectModel/properties/IsOneToOneMapping with value: 'map[description:Map user account to exact robot account type:boolean]'
  - Added /ProjectExt/properties/IsOneToOneMapping with value: 'map[description:Map user account to exact robot account type:boolean]'
  - Added /SubmittedTaskInfoExt/properties/Reason with value: 'map[description:Reason (parsed from scheduler, e.g. SLURM) type:string]'
  - Removed /TaskSpecificationExt/properties/TaskParalizationParameters with value: 'map[description:Array of task paralization parameters items:map[$ref:#/definitions/TaskParalizationParameterExt] type:array]'
  - Added /TaskSpecificationExt/properties/TaskParallelizationParameters with value: 'map[description:Array of task paralelization parameters items:map[$ref:#/definitions/TaskParalizationParameterExt] type:array]'
  - Added /VaultInfo_ with value: 'map[additionalProperties:false properties:map[Initialized:map[description:Vault is initialized type:boolean] PerformanceStandby:map[description:Vault is in performance stand by type:boolean] Sealed:map[description:Vault is sealed type:boolean] StandBy:map[description:Vault is in stand by type:boolean]] type:object]'
  - Added /Vault_ with value: 'map[additionalProperties:false properties:map[Info:map[$ref:#/definitions/VaultInfo_] IsHealthy:map[description:Vault is healthy type:boolean]] type:object]'




## V5.0.0

### Changed
- Updated to the .NET 9 
- Allowed to `DeleteJob` in state `Configuring` or `WaitingForServiceAccount`

### Added
- Automatic docker compose Vault initialization and unsealing procedure
- Propagation of JobState `Deleted` into `JobSpecification`
- HPC job archive option at `DeleteJob` endpoint
- Job Log archive access from `FileTransfer` endpoints
- `ListChangedFilesForJob` endpoint job logs archive listing when HPC Job deleted and archived
- Feature to enable or disable `CommandTemplate` by `IsEnabled` property
- Advanced filter for `ListavailableClusters` endpoint
- JobState filter for `ListJobsForCurrentUser`
- Management endpoints for HEAppE entities

### Fixed
- An issue where creating and submitting a job with `MaxCores` missing
- Concurrent auth token evaluation issue and role mapping (by single user)
- Swagger models description


## V4.3.0

### Added
- Hashicorp Credentials Vault implementation as secure storage for HPC credentials
- Scripts to initialize vault and migrate data from database to vault
- SubProjects for logical organization of the Jobs under the Projects 
- SubProject management via API
- Aggregation of the Cluster Node Types
- Accounting & Reporting by specific accounting formula
- Asynchronous accounting computation endpoint and accounting state monitoring endpoint

### Fixed
- Wrong relative path propagation as result of `FileTransfer/ListChangedFilesForJob` endpoint when using `ClusterTaskSubdirectory`
- Internal error when cancelling jobs
- Internal error when using an expired session code

### Changed
- Enhanced JobReporting endpoints outputs and ability of filtering
- `JobManagement/CreateJob` endpoint body extended by optional parameter `SubProjectIdentifier` in the `JobSpecification` section

## V4.2.1

### Added
- Command Template management via API

### Fixed
- Initialize Cluster Script Directory method
- LEXIS V2 AAI
- Problem with SSH connection (.SSH Net Library)
- Cancel job functionality in exclusive mode

### Changed
- Session exception handling

### Security
- Packages update due to vulnerability

## V4.2.0

### Added
- HyperQueue scheduler support
- API Project Management support
- LEXIS V2 AAI support
- Shared pool mode support
- Cluster Scripts initialization via API
- Enhanced Exception handling 
- Enhanced HPC project information
- User role management improvement

### Fixed
- SFTP interactive mode
- HPC Identities rotation

## V4.1.1

### Fixed
- Fix Regex

## V4.1.0

### Added
- Adding SSH key for HPC identity via API
- DataStaging independent API endpoint

## V4.0.0

### Added
- Multi-HPC projects support
- SLURM scheduler QOS attribute support

### Changed
- API methods updated

## V3.1.1

### Fixed
- Update documentation

## V3.1.0

### Added
- API Memory caching for ClusterInformation Endpoint
- Database logging support

### Changed
- Job Reporting methods extended
- File transfer module updated

## V3.0.0

### Added
- Support for local computing (HPC cluster simulation)
- Management roles extension
- SLURM scheduler partition support

### Changed
- HPC schedulers layer refactor

### Security
- Security update in relation to Keycloak and OpenStack module

## V2.3.0

### Added
- Two options for HPC cluster accounts utilization

## V2.2.0

### Added
- Generic command template support: Users with direct access to HPC infrastructure can create and test their job scripts directly on the cluster system and then run it via HEAppE's generic command template job remotely without the need to setup the new dedicated command template

## V2.1.0

### Added
- ExtraLong job support:
HEAppE enables the execution of extra-long jobs that would normally exceed the maximum walltime parametr of the longest scheduler queue. When the requested job walltime exceeds the queue's maximum walltime the submitted job is automatically divided into a number of smaller ones with the dependency support.
- OpenMPI & MPI support
