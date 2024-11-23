using System;
using System.IO;
using System.Text;

using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto.Utilities;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.Utilities.IO.Pem;

namespace HEAppE.CertificateGenerator.Generators.v2
{
    public class ECDsaCertGeneratorV2 : GenericCertGeneratorV2
    {
        #region Instances
        private int _keySize;
        #endregion

        /// <summary>
        /// Initializes a new instance of the ECDSACertGeneratorV2 class with the specified curve name (default: "secp256r1").
        /// </summary>
        public ECDsaCertGeneratorV2()
        {
            _keySize = 521;
            _generator = new ECKeyPairGenerator("ECDSA");
            var secureRandom = new SecureRandom();
            _generator.Init(GetCurveParameters(secureRandom, _keySize));
            _keyPair = _generator.GenerateKeyPair();
        }

        /// <summary>
        /// Initializes a new instance of the ECDSACertGeneratorV2 class with the specified curve name.
        /// </summary>
        /// <param name="keySize">Size of the key</param>
        public ECDsaCertGeneratorV2(int keySize)
        {
            _keySize = keySize;
            _generator = new ECKeyPairGenerator("ECDSA");
            var secureRandom = new SecureRandom();
            _generator.Init(GetCurveParameters(secureRandom, _keySize));
            _keyPair = _generator.GenerateKeyPair();
        }

        /// <summary>
        /// Regenerates the EC key pair.
        /// </summary>
        public override void Regenerate()
        {
            _keyPair = _generator.GenerateKeyPair();
        }

        /// <summary>
        /// Converts the private key to an encrypted PEM format.
        /// </summary>
        /// <param name="passphrase">The passphrase to encrypt the private key.</param>
        /// <param name="cipherAlgorithm">The cipher algorithm to use for encryption (default: AES-128-CBC).</param>
        /// <returns>The private key in encrypted PEM format.</returns>
        public override string ToEncryptedPrivateKeyInPEM(string passphrase, string cipherAlgorithm = "AES-128-CBC")
        {
            //throw new NotSupportedException("Encryption of ECDSA private keys is not supported.");
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
            ECPrivateKeyParameters privateKey = _keyPair.Private as ECPrivateKeyParameters;
            pemWriter.WriteObject(privateKey);
            pemWriter.Writer.Flush();
            return stringWriter.ToString();
        }

        /// <summary>
        /// Converts the public key to OpenSSH format for the authorized keys.
        /// </summary>
        /// <param name="comment"></param>
        /// <returns></returns>
        public override string ToPublicKeyInAuthorizedKeysFormat(string comment = null)
        {
            //throw new NotSupportedException("Conversion is not supported");
            byte[] publicKeyBytes = OpenSshPublicKeyUtilities.EncodePublicKey(_keyPair.Public);
            string base64PublicKey = Convert.ToBase64String(publicKeyBytes);

            StringBuilder formattedPublicKey = new StringBuilder();
            formattedPublicKey.Append($"ecdsa-sha2-nistp{_keySize} ");
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
        /// Returns fingerprint of the public key in SHA256 format
        /// </summary>
        /// <returns></returns>
        public override string GetPublicKeyFingerprint()
        {
            byte[] publicKeyBytes = OpenSshPublicKeyUtilities.EncodePublicKey(_keyPair.Public);
            byte[] fingerprintBytes;


            fingerprintBytes = DigestUtilities.CalculateDigest("SHA256", publicKeyBytes);
            return BitConverter.ToString(fingerprintBytes).Replace("-", string.Empty).ToLower();
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

        /// <summary>
        /// Returns generated curve parameters
        /// </summary>
        /// <param name="secureRandom"></param>
        /// <param name="size"></param>
        /// <returns></returns>
        private KeyGenerationParameters GetCurveParameters(SecureRandom secureRandom, int size)
        {
            var keyGenParameters = new KeyGenerationParameters(secureRandom, size);
            return keyGenParameters;
        }

        public static string ToPublicKeyInAuthorizedKeysFormatFromPrivateKey(string privateKey,
            string passphrase, string comment = null)
        {
            using var fileStream = new StringReader(privateKey);
            var pemReader = new Org.BouncyCastle.OpenSsl.PemReader(fileStream, new PasswordFinder(passphrase));
            var keyPair = pemReader.ReadObject() as AsymmetricCipherKeyPair;
            var publicKey = keyPair.Public;
            byte[] publicKeyBytes = OpenSshPublicKeyUtilities.EncodePublicKey(publicKey);
            string base64PublicKey = Convert.ToBase64String(publicKeyBytes);

            //get key size from private key
            var privateKeyFromPair = keyPair.Private;
            ECPrivateKeyParameters privateKeyParams = privateKeyFromPair as ECPrivateKeyParameters;
            int keySize = privateKeyParams.Parameters.Curve.FieldSize;

            StringBuilder formattedPublicKey = new StringBuilder();
            formattedPublicKey.Append($"ecdsa-sha2-nistp{keySize} ");
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
    }
}
