using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto.Utilities;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.Utilities.IO.Pem;
using System;
using System.IO;
using System.Text;

namespace HEAppE.CertificateGenerator.Generators.v2
{
    public class RSACertGeneratorV2 : GenericCertGeneratorV2
    {
        #region Instances
        /// <summary>
        /// Size
        /// </summary>
        private readonly int _size;
        #endregion
        /// <summary>
        /// Initializes a new instance of the RSACertGeneratorV2 class with a default key size of 4096 bits.
        /// </summary>
        public RSACertGeneratorV2()
        {
            _size = 4096;
            _generator = new RsaKeyPairGenerator();
            _generator.Init(new KeyGenerationParameters(new SecureRandom(), _size));
            _keyPair = _generator.GenerateKeyPair();
        }

        /// <summary>
        /// Initializes a new instance of the RSACertGeneratorV2 class with the specified key size.
        /// </summary>
        /// <param name="size">The size of the RSA key in bits.</param>
        public RSACertGeneratorV2(int size)
        {
            _size = size;
            _generator = new RsaKeyPairGenerator();
            _generator.Init(new KeyGenerationParameters(new SecureRandom(), _size));
            _keyPair = _generator.GenerateKeyPair();
        }

        /// <summary>
        /// Regenerates the RSA key pair.
        /// </summary>
        public override void Regenerate()
        {
            _keyPair = _generator.GenerateKeyPair();
        }

        /// <summary>
        /// Converts the public key to the authorized_keys format.
        /// </summary>
        /// <returns></returns>
        public override string ToPublicKeyInAuthorizedKeysFormat(string comment = null)
        {
            byte[] publicKeyBytes = OpenSshPublicKeyUtilities.EncodePublicKey(_keyPair.Public);
            string base64PublicKey = Convert.ToBase64String(publicKeyBytes);

            StringBuilder formattedPublicKey = new StringBuilder();
            formattedPublicKey.Append("ssh-rsa ");
            formattedPublicKey.Append(base64PublicKey);

            if (!string.IsNullOrEmpty(comment))
            {
                formattedPublicKey.Append($" {comment}");
            }
            else
            {
                formattedPublicKey.Append($" {_publicComment}");
            }

            return formattedPublicKey.ToString();
        }

        /// <summary>
        /// Returns Public Key Fingerprint in the SHA256 algorithm.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public override string GetPublicKeyFingerprint()
        {
            byte[] publicKeyBytes = OpenSshPublicKeyUtilities.EncodePublicKey(_keyPair.Public);
            byte[] fingerprintBytes;

            fingerprintBytes = DigestUtilities.CalculateDigest("SHA256", publicKeyBytes);
            return BitConverter.ToString(fingerprintBytes).Replace("-", string.Empty).ToLower();
        }

        /// <summary>
        /// Converts the private key to an encrypted PEM format.
        /// </summary>
        /// <param name="passphrase">The passphrase to encrypt the private key.</param>
        /// <param name="cipherAlgorithm">The cipher algorithm to use for encryption (default: AES-128-CBC).</param>
        /// <returns>The private key in encrypted PEM format.</returns>
        public override string ToEncryptedPrivateKeyInPEM(string passphrase, string cipherAlgorithm = "AES-128-CBC")
        {
            StringWriter stringWriter = new StringWriter();
            Org.BouncyCastle.OpenSsl.PemWriter pemWriter = new Org.BouncyCastle.OpenSsl.PemWriter(stringWriter);
            var privateKey = _keyPair.Private;
            pemWriter.WriteObject(privateKey, cipherAlgorithm, passphrase.ToCharArray(), SecureRandom.GetInstance("SHA256PRNG"));
            pemWriter.Writer.Flush();
            return stringWriter.ToString();
        }

        /// <summary>
        /// Converts the private key to PEM format.
        /// </summary>
        /// <returns>The private key in PEM format.</returns>
        public override string ToPrivateKeyInPEM()
        {
            StringWriter stringWriter = new StringWriter();
            Org.BouncyCastle.OpenSsl.PemWriter pemWriter = new Org.BouncyCastle.OpenSsl.PemWriter(stringWriter);
            RsaPrivateCrtKeyParameters privateKey = _keyPair.Private as RsaPrivateCrtKeyParameters;
            pemWriter.WriteObject(privateKey);
            pemWriter.Writer.Flush();
            return stringWriter.ToString();
        }
        public new static string ToPublicKeyInAuthorizedKeysFormatFromPrivateKey(string privateKeyPath,
            string passphrase, string comment= null)
        {
            var fileStream = System.IO.File.OpenText(privateKeyPath);
            var pemReader = new Org.BouncyCastle.OpenSsl.PemReader(fileStream, new PasswordFinder(passphrase));
            var keyPair = pemReader.ReadObject() as AsymmetricCipherKeyPair;
            var publicKey = keyPair.Public;
            byte[] publicKeyBytes = OpenSshPublicKeyUtilities.EncodePublicKey(publicKey);
            string base64PublicKey = Convert.ToBase64String(publicKeyBytes);

            StringBuilder formattedPublicKey = new StringBuilder();
            formattedPublicKey.Append("ssh-rsa ");
            formattedPublicKey.Append(base64PublicKey);
            
            if (!string.IsNullOrEmpty(comment))
            {
                formattedPublicKey.Append($" {comment}");
            }
            else
            {
                formattedPublicKey.Append($" {_publicComment}");
            }
            return formattedPublicKey.ToString();
        }

        /// <summary>
        /// Converts the public key to PEM format.
        /// </summary>
        /// <returns>The public key in PEM format.</returns>
        public override string ToPublicKeyInPEM()
        {
            byte[] publicKeyBytes = OpenSshPublicKeyUtilities.EncodePublicKey(_keyPair.Public);
            using (StringWriter stringWriter = new StringWriter())
            {
                using (Org.BouncyCastle.OpenSsl.PemWriter w = new Org.BouncyCastle.OpenSsl.PemWriter(stringWriter))
                {
                    w.WriteObject(new PemObject("OPENSSH PUBLIC KEY", publicKeyBytes));
                }
                return stringWriter.ToString();
            }
        }
    }
}