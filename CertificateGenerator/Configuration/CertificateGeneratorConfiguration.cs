namespace HEAppE.CertificateGenerator.Configuration
{
    /// <summary>
    /// Certificate generator configuration
    /// </summary>
    public sealed class CertificateGeneratorConfiguration
    {
        #region Properties
        /// <summary>
        /// Directory for generated SSH keys
        /// </summary>
        public static string GeneratedKeysDirectory { get; set; }
        /// <summary>
        /// Cipher generation configuration
        /// </summary>
        public static CipherGeneratorConfiguration CipherSettings { get; } = new CipherGeneratorConfiguration();
        #endregion
    }
}
