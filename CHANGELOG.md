# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## V4.2.2

### Added
- Hashicorp Credentials Vault implementation as secure storage for HPC credentials
- Scripts to initialize vault and migrate data from database to vault
- SubProjects for logical organisation of the Jobs under the Projects 
- SubProject management via API
- Aggregation of the Cluster Node Types
- Accounting & Reporting by specific accounting formula
- Asynchronous accounting computation endpoint and accounting state monitoring endpoint

### Fixed
- Wrong relative path propagation as result of `FileTransfer/ListChangedFilesForJob` endpoint when using `ClusterTaskSubdirectory`

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
