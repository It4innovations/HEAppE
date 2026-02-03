using log4net;

namespace HEAppE.Utils;

/// <summary>
///     Logging utility class
/// </summary>
public static class LoggingUtils
{
    private const string LOG_PROPERTY_USER_ID_PROPERTY = "userId";
    private const string LOG_PROPERTY_USER_NAME_PROPERTY = "userName";
    private const string LOG_PROPERTY_USER_EMAIL_PROPERTY = "userEmail";
    private const string LOG_PROPERTY_JOB_ID_PROPERTY = "jobId";

    /// <summary>
    ///     Add user properties to log thread context
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="userName"></param>
    /// <param name="email"></param>
    public static void AddUserPropertiesToLogThreadContext(long userId, string userName, string email)
    {
        if (userId > 0)
            LogicalThreadContext.Properties[LOG_PROPERTY_USER_ID_PROPERTY] = userId;
        if (!string.IsNullOrEmpty(userName))
            LogicalThreadContext.Properties[LOG_PROPERTY_USER_NAME_PROPERTY] = userName;
        if (!string.IsNullOrEmpty(email))
            LogicalThreadContext.Properties[LOG_PROPERTY_USER_EMAIL_PROPERTY] = email;
    }

    /// <summary>
    ///     Remove user properties from log thread context
    /// </summary>
    public static void RemoveUserPropertiesFromLogThreadContext()
    {
        LogicalThreadContext.Properties.Remove(LOG_PROPERTY_USER_ID_PROPERTY);
        LogicalThreadContext.Properties.Remove(LOG_PROPERTY_USER_NAME_PROPERTY);
        LogicalThreadContext.Properties.Remove(LOG_PROPERTY_USER_EMAIL_PROPERTY);
    }

    /// <summary>
    ///     Add job id to log thread context
    /// </summary>
    /// <param name="jobId"></param>
    public static void AddJobIdToLogThreadContext(long jobId)
    {
        LogicalThreadContext.Properties[LOG_PROPERTY_JOB_ID_PROPERTY] = jobId;
    }

    /// <summary>
    ///     Remove job id from log thread context
    /// </summary>
    public static void RemoveJobIdFromLogThreadContext()
    {
        LogicalThreadContext.Properties.Remove(LOG_PROPERTY_JOB_ID_PROPERTY);
    }
}
