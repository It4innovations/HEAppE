using System;

namespace HEAppE.DomainObjects.Notifications
{
    [Flags]
    public enum NotificationEvent
    {
        AdministrationUserCreated = 1,
        AdministrationUserCredentialChange = 2,
        AdministrationUserDeleted = 4,
        AdministrationUserEmailAfterChange = 8,
        AdministrationUserEmailBeforeChange = 16,
        EmptyMonthlyUsageReport = 32,
        JobAborted = 64,
        JobFinished = 128,
        JobStarted = 256,
        MonthlyUsageReport = 516
    }
}