namespace HEAppE.DomainObjects.Logging;

public enum SystemEventType
{
    AdaptorUserAuthenticated,
    AdaptorUsersSynchronized,
    AdministrationUserCreated,
    AdministrationUserCredentialsChanged,
    AdministrationUserDeleted,
    AdministrationUserLoggedIn,
    AdministrationUserUpdated,
    FileTransferMethodRequest,
    InputFilesUpload,
    JobCanceled,
    JobFileSynchronization,
    JobFileUploaded,
    JobSubmitted,
    ResultFilesDownload,
    SessionFilesDeleted,
    UsageLimitationsUpdated
}