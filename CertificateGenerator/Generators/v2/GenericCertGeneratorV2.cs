﻿using Org.BouncyCastle.Crypto;
using System;
using System.IO;
using Org.BouncyCastle.Crypto.Utilities;
using Org.BouncyCastle.Utilities.IO.Pem;

namespace HEAppE.CertificateGenerator.Generators.v2
{
    public abstract class GenericCertGeneratorV2
    {
        #region Instances
        /// <summary>
        /// Asymmetric cipher key pair
        /// </summary>
        protected AsymmetricCipherKeyPair _keyPair;

        /// <summary>
        /// BouncyCastle RSA key generator
        /// </summary>
        protected IAsymmetricCipherKeyPairGenerator _generator;
        #endregion

        #region Protected Properties
        /// <summary>
        /// Public comment
        /// </summary>
        protected static string _publicComment = $"key-added-{DateTime.UtcNow.ToString("yyyy-MM-dd_HH-mm-ss")}";
        #endregion


        #region Abstract Methods
        /// <summary>
        /// Re-Generate key
        /// </summary>
        public abstract void Regenerate();

        /// <summary>
        /// Returns the SSH private key
        /// </summary>
        /// <returns></returns>
        public abstract string ToPrivateKeyInPEM();

        /// <summary>
        /// Returns the encrypted SSH private key
        /// </summary>
        /// <param name="passphrase"></param>
        /// <returns></returns>
        public abstract string ToEncryptedPrivateKeyInPEM(string passphrase, string cipherAlgorithm = "AES-128-CBC");

        /// <summary>
        /// Returns the SSH public key
        /// </summary>
        /// <returns></returns>
        public abstract string ToPublicKeyInPEM();

        public static string ToPublicKeyInPEMFromPrivateKey(string privateKeyPath, string passphrase)
        {
            var fileStream = System.IO.File.OpenText(privateKeyPath);
            var pemReader = new Org.BouncyCastle.OpenSsl.PemReader(fileStream, new PasswordFinder(passphrase));
            var keyPair = pemReader.ReadObject() as AsymmetricCipherKeyPair;
            var publicKey = keyPair.Public;
            byte[] publicKeyBytes = OpenSshPublicKeyUtilities.EncodePublicKey(publicKey);
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
        /// Returns the SSH public key in OpenSSH authorized_keys format
        /// </summary>
        /// <returns></returns>
        public abstract string ToPublicKeyInAuthorizedKeysFormat(string comment = null);

        /// <summary>
        /// Returns Public Key Fingerprint
        /// </summary>
        /// <param name="fingerprintAlgorithm"></param>
        /// <returns></returns>
        public abstract string GetPublicKeyFingerprint();
        #endregion
    }
}
