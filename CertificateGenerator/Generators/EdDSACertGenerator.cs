using System.IO;
using System;
using System.Text;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Crypto.Utilities;

namespace HEAppE.CertificateGenerator.Generators;

/// <summary>
///     EdDSA Cipher
/// </summary>
public class EdDSACertGenerator : GenericCertGenerator
{
    #region Instances

    private AsymmetricCipherKeyPair _keyPair;

    #endregion

    #region Constructors

    /// <summary>
    ///     Constructor
    /// </summary>
    public EdDSACertGenerator()
    {
        GenerateKeyPair();
    }

    /// <summary>
    ///     Constructor
    /// </summary>
    /// <param name="comment">Comment</param>
    public EdDSACertGenerator(string comment)
    {
        GenerateKeyPair();
        _publicComment = comment;
    }

    #endregion

    #region Public Methods

    /// <summary>
    ///     Re-Generate key
    /// </summary>
    public override void Regenerate()
    {
        GenerateKeyPair();
    }

    /// <summary>
    ///     Returns the SSH private key
    /// </summary>
    /// <returns></returns>
    public override string ToPrivateKey()
    {
        using var stringWriter = new StringWriter();
        using var pemWriter = new PemWriter(stringWriter);
        var privateKey = _keyPair.Private as Ed25519PrivateKeyParameters;
        pemWriter.WriteObject(privateKey);
        pemWriter.Writer.Flush();
        return stringWriter.ToString();
    }

    /// <summary>
    ///     Returns the SSH public key
    /// </summary>
    /// <returns></returns>
    public override string ToPublicKey()
    {
        using var stringWriter = new StringWriter();
        using var pemWriter = new PemWriter(stringWriter);
        var publicKey = _keyPair.Public as Ed25519PublicKeyParameters;
        pemWriter.WriteObject(publicKey);
        pemWriter.Writer.Flush();
        return stringWriter.ToString();
    }

    /// <summary>
    ///     Returns the SSH public key in PuTTY format
    /// </summary>
    /// <returns></returns>
    public override string ToPuTTYPublicKey()
    {
        var publicKeyBytes = OpenSshPublicKeyUtilities.EncodePublicKey(_keyPair.Public);
        var base64PublicKey = Convert.ToBase64String(publicKeyBytes);

        var formattedPublicKey = new StringBuilder();
        formattedPublicKey.Append("ssh-ed25519 ");
        formattedPublicKey.Append(base64PublicKey);
        formattedPublicKey.Append($" {_publicComment}");

        return formattedPublicKey.ToString();
    }

    #endregion

    #region Private methods

    private void GenerateKeyPair()
    {
        var keyGen = new Ed25519KeyPairGenerator();
        keyGen.Init(new Ed25519KeyGenerationParameters(new SecureRandom()));
        _keyPair = keyGen.GenerateKeyPair();
    }

    #endregion
}
