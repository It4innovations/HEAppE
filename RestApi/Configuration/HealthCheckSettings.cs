namespace HEAppE.RestApi.Configuration;

/// <summary>
///     Application API configuration
/// </summary>
public sealed class HealthCheckSettings
{
    #region Properties

    /// <summary>
    ///    health checks cache expiration in milliseconds
    /// </summary>
    public static int HealthChecksCacheExpirationMs { get; set; } = 5000;

    #endregion
}