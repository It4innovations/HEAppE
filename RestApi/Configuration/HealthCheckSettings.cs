namespace HEAppE.RestApi.Configuration;

/// <summary>
///     Application API configuration
/// </summary>
public sealed class HealthCheckSettings
{
    #region Properties

    /// <summary>
    ///    heappe/Management/Health endpoint's cache expiration in milliseconds
    /// </summary>
    public static int ManagementHealthCacheExpirationMs { get; set; } = 5000;

    #endregion
}