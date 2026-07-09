using System;

namespace SshCaAPI.Configuration
{
    public class SshCaSettings
    {
        public static Func<string, string, string?>? ConfigResolver { get; set; }

        private static T GetValue<T>(string propertyName, T defaultValue)
        {
            if (ConfigResolver != null)
            {
                var val = ConfigResolver("SshCaSettings", propertyName);
                if (val != null)
                {
                    try
                    {
                        return (T)Convert.ChangeType(val, typeof(T));
                    }
                    catch
                    {
                        // Fall back to default
                    }
                }
            }
            return defaultValue;
        }

        /// <summary>
        ///     Client base URI
        /// </summary>
        private static string _baseUri = "http://localhost";
        public static string BaseUri
        {
            get => GetValue(nameof(BaseUri), _baseUri);
            set => _baseUri = value;
        }
        
        /// <summary>
        ///    Use certificate authority for authentication
        /// </summary>
        private static bool _useCertificateAuthorityForAuthentication = false;
        public static bool UseCertificateAuthorityForAuthentication
        {
            get => GetValue(nameof(UseCertificateAuthorityForAuthentication), _useCertificateAuthorityForAuthentication);
            set => _useCertificateAuthorityForAuthentication = value;
        }
        
        private static bool _usePosixAccountFromCertificate = true;
        public static bool UsePosixAccountFromCertificate
        {
            get => GetValue(nameof(UsePosixAccountFromCertificate), _usePosixAccountFromCertificate);
            set => _usePosixAccountFromCertificate = value;
        }

        /// <summary>
        ///     Certification authority name
        /// </summary>
        private static string _caName = string.Empty;
        public static string CAName
        {
            get => GetValue(nameof(CAName), _caName);
            set => _caName = value;
        }

        /// <summary>
        ///     Client authentication token (only used for testing purpose)
        /// </summary>
        private static string? _token;
        public static string? Token
        {
            get => GetValue(nameof(Token), _token);
            set => _token = value;
        }

        /// <summary>
        ///     Client connection timeout in seconds
        /// </summary>
        private static double _connectionTimeoutInSeconds = 15;
        public static double ConnectionTimeoutInSeconds
        {
            get => GetValue(nameof(ConnectionTimeoutInSeconds), _connectionTimeoutInSeconds);
            set => _connectionTimeoutInSeconds = value;
        }
    }
}
