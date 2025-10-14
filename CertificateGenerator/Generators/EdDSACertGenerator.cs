using System.IO;
using System;
using System.Net;
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
        var pub = (Ed25519PublicKeyParameters)_keyPair.Public;
        var keyType = Encoding.ASCII.GetBytes("ssh-ed25519");
        var pubRaw = pub.GetEncoded();
        using var ms = new MemoryStream();
        using (var bw = new BinaryWriter(ms))
        {
            bw.Write(IPAddress.HostToNetworkOrder(keyType.Length));
            bw.Write(keyType);
            bw.Write(IPAddress.HostToNetworkOrder(pubRaw.Length));
            bw.Write(pubRaw);
        }
        var base64 = Convert.ToBase64String(ms.ToArray());
        string comment = $"key-temp-added-{DateTime.UtcNow.ToString("yyyy-MM-dd_HH-mm-ss")}";
        return $"ssh-ed25519 {base64} {comment}";
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
