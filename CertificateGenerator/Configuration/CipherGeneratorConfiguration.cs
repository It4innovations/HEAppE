using System;

namespace HEAppE.CertificateGenerator.Configuration
{
    /// <summary>
    /// SSH key generator configuration
    /// </summary>
    public sealed class CipherGeneratorConfiguration
    {
        #region Instances
        /// <summary>
        /// SSH cipher type name
        /// </summary>
        private static string _typeName;

        /// <summary>
        /// SSH cipher type
        /// </summary>
        private static CipherType _type;
        #endregion
        #region Properties
        /// <summary>
        /// SSH cipher type name
        /// </summary>
        public static string TypeName
        {
            set
            {
                _typeName = value.ToUpper();
            }
        }

        /// <summary>
        /// SSH cipher size
        /// </summary>
        public static int Size { private get;  set; }

        /// <summary>
        /// SSH cipher type
        /// </summary>
        public static CipherType Type
        {
            get
            {
                if (_type == default)
                {
                    SetCipherType();
                }

                return _type;
            }
        }
        #endregion
        #region Private Members
        /// <summary>
        /// Set SSH cipher type
        /// </summary>
        private static void SetCipherType()
        {
            _type = (_typeName, Size) switch
            {
                ("RSA", 3072) => CipherType.RSA3072,
                ("RSA", 4096) => CipherType.RSA4096,
                ("ECDSA", 256) => CipherType.nistP256,
                ("ECDSA", 521) => CipherType.nistP521,
                _ => CipherType.Unknown
            };
        }
        #endregion
    }
}
