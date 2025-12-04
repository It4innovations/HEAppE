namespace Services.Expirio.Configuration
{
    public class ExpirioSettings
    {
        /// <summary>
        ///     Client base URI
        /// </summary>
        public static string BaseUrl { get; set; } = "localhost";

        /// <summary>
        ///     Client connection timeout in seconds
        /// </summary>
        public static int TimeoutSeconds { get; set; } = 30;

        /// <summary>
        ///     Client maximum number of connection retries
        /// </summary>
        public static int MaxRetries { get; set; } = 3;
        
        /// <summary>
        ///     Client delay for connection retry
        /// </summary>
        public static int RetryInitialDelayMs { get; set; } = 200;

        /// <summary>
        ///     Client authentication token (only used for testing purpose)
        /// </summary>
        //public static string? Token { get; set; }
    }
}