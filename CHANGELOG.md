# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).


## V5.1.0

### Changed
- Enhanced logging in `DataTransfer` endpoints
- Redefined `Job Execution` and `Job Log Archive path` (dedicated to specific HEAppE instance and cluster user)
- Redefined path to `cluster scripts` and setup for each `ClusterAuthenticationCredential` and `Project`
- `Management/InitializeClusterScriptDirectory` body and business logic
- Response structure of the `TestClusterAccessForAccount` (added info/check about specific access to the cluster)

### Added
- 1:1 user mapping to SSH key at `Project` level
- Possibility to send `application/json` payload with `HttpPostToJobNode`
- Support for the `EdDSA - ED25519` SSH key pair generation
- Options `ConnectionRetryAttempts` and `ConnectionTimeout` for SSH client component are now configurable from `appsettings.json`
- API HTTP Request logging with payload (redacted output on `Sensitive data`)
- `Reason` attribute propagation from the HPC job (in the HEAppE Task)
- `IsInitialized` attribute for `ClusterAuthenticationCredentials` with check for all endpoints which uses `ClusterAuthenticationCredential` to connect HPC
- Endpoints for bulk listing of `ClusterNodeTypes`, `FileTransferMethods`, `Projects`, `ClusterNodeTypeAggregationAccountings`

### Fixed
- `External UsageType` model conversion to `Internal UsageType` model (enum)
- Fixed wrong error messages for CommandTemplateParameters methods in ManagementService
- Typo in TaskParallelizationParameters in the `HEAppE Task` specificaton in `CreateJob` endpoint

### Open-API changes summary
- edit-summary                  /heappe/Management/CommandTemplateParameter (get) - Summary turned from Get CommandTemplateParameter by id to Get Command Template Parameter by id
- edit-summary                  /heappe/Management/CommandTemplateParameter (post) - Summary turned from Create Static Command Template to Create a new Command Template Parameter
- edit-summary                  /heappe/Management/CommandTemplateParameter (put) - Summary turned from Modify Static Command Template to Modify an existing Command Template Parameter
- edit-summary                  /heappe/Management/CommandTemplateParameter (delete) - Summary turned from Remove Static Command Template to Remove an existing Command Template Parameter
- add-description               /paths//heappe/Management/SecureShellKeys/get/parameters/ProjectId/ - Description added: 
- add-description               /paths//heappe/Management/SecureShellKeys/get/parameters/SessionCode/ - Description added: 
- add-path                      /heappe/Management/Projects - Added
- add-path                      /heappe/Management/ProjectAssignmentToClusters - Added
- add-path                      /heappe/Management/ClusterNodeTypes - Added
- add-path                      /heappe/Management/ClusterProxyConnections - Added
- add-path                      /heappe/Management/FileTransferMethods - Added
- add-path                      /heappe/Management/ClusterNodeTypeAggregationAccountings - Added
- add-path                      /heappe/Management/Accountings - Added
- add-optional-object-property  definitions/ClusterExt - Optional property FileTransferMethodIds added
- add-optional-object-property  definitions/ClusterNodeTypeExt - Optional property ClusterId added
- add-optional-object-property  definitions/CreateProjectModel - Optional property IsOneToOneMapping added
- add-optional-object-property  definitions/ExtendedClusterExt - Optional property FileTransferMethodIds added
- add-optional-object-property  definitions/FileTransferMethodNoCredentialsExt - Optional property ClusterId added
- delete-object-property        definitions/InitializeClusterScriptDirectoryModel - Property ClusterProjectRootDirectory deleted
- add-optional-object-property  definitions/InitializeClusterScriptDirectoryModel - Optional property OverwriteExistingProjectRootDirectory added
- add-optional-object-property  definitions/InitializeClusterScriptDirectoryModel - Optional property Username added
- add-optional-object-property  definitions/ModifyProjectModel - Optional property IsOneToOneMapping added
- add-optional-object-property  definitions/ProjectExt - Optional property IsOneToOneMapping added
- add-optional-object-property  definitions/SubmittedTaskInfoExt - Optional property Reason added
- delete-object-property        definitions/TaskSpecificationExt - Property TaskParalizationParameters deleted
- add-optional-object-property  definitions/TaskSpecificationExt - Optional property TaskParallelizationParameters added
- add-definition                ClusterAccessReportExt - Added

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
