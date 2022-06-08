using HEAppE.CertificateGenerator.Configuration;
using HEAppE.CertificateGenerator.Generators;
using System;

namespace HEAppE.CertificateGenerator
{
    /// <summary>
    /// Generator
    /// </summary>
    public class SSHGenerator
    {
        #region Instances
        /// <summary>
        /// Cipher key
        /// </summary>
        private readonly GenericCertGenerator _key;
        #endregion
        #region Constructors
        /// <summary>
        /// Construcotr
        /// </summary>
        /// <exception cref="NotImplementedException"></exception>
        public SSHGenerator()
        {
            _key = CipherGeneratorConfiguration.Type switch
            {
                CipherType.RSA2048 => new RSACertGenerator(2048),
                CipherType.RSA3072 => new RSACertGenerator(3072),
                CipherType.RSA4096 => new RSACertGenerator(4096),
                CipherType.nistP256 => new ECDsaCertGenerator("nistP256"),
                CipherType.nistP521 => new ECDsaCertGenerator("nistP521"),
                _ => new RSACertGenerator(4096)
            };
        }
        #endregion
        #region Methods
        /// <summary>
        /// Returns the SSH private key
        /// </summary>
        /// <returns></returns>
        public string ToPrivateKey()
        {
            return _key.ToPrivateKey();
        }

        /// <summary>
        /// Returns the SSH public key
        /// </summary>
        /// <returns></returns>
        public string ToPublicKey()
        {
            return ToPublicKey();
        }

        /// <summary>
        /// Returns the SSH public key in PuTTY format
        /// </summary>
        /// <returns></returns>
        public string ToPuTTYPublicKey()
        {
            return ToPuTTYPublicKey();
        }
        #endregion
    }
}
