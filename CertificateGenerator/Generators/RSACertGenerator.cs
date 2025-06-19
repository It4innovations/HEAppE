using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using HEAppE.Exceptions.Internal;

namespace HEAppE.CertificateGenerator.Generators;

/// <summary>
///     RSA Cipher
/// </summary>
public class RSACertGenerator : GenericCertGenerator
{
    #region Instances

    /// <summary>
    ///     Size
    /// </summary>
    private readonly int _size;

    #endregion

    #region Constructors

    /// <summary>
    ///     Constructor
    /// </summary>
    public RSACertGenerator()
    {
        _size = 4096;
        _key = RSA.Create(_size);
    }

    /// <summary>
    ///     Constructor
    /// </summary>
    /// <param name="size">Size</param>
    public RSACertGenerator(int size)
    {
        if (size % 512 != 0) throw new SshClientArgumentException("KeyGenerationException", "RSA", "n * 1024");

        _size = size;
        _key = RSA.Create(_size);
    }

    /// <summary>
    ///     Constructor
    /// </summary>
    /// <param name="size">Size</param>
    /// <param name="comment">Comment</param>
    public RSACertGenerator(int size, string comment)
    {
        if (size % 512 != 0) throw new SshClientArgumentException("KeyGenerationException", "RSA", "n * 1024");

        _size = size;
        _key = RSA.Create(_size);
        _publicComment = comment;
    }

    #endregion

    #region Public Methods

    /// <summary>
    ///     Re-Generate key
    /// </summary>
    public override void Regenerate()
    {
        _key = RSA.Create(_size);
    }

    /// <summary>
    ///     Returns the SSH private key
    /// </summary>
    /// <returns></returns>
    public override string ToPrivateKey()
    {
        StringBuilder builder = new();
        _ = builder.AppendLine("-----BEGIN RSA PRIVATE KEY-----");
        var privateKeyBytes = Convert.ToBase64String(((RSA)_key).ExportRSAPrivateKey()).ToCharArray();
        for (var i = 0; i < privateKeyBytes.Length; i += 64)
            _ = builder.AppendLine(new string(privateKeyBytes, i, Math.Min(64, privateKeyBytes.Length - i)));
        _ = builder.AppendLine("-----END RSA PRIVATE KEY-----");
        return builder.ToString();
    }

    /// <summary>
    ///     Returns the SSH public key
    /// </summary>
    /// <returns></returns>
    public override string ToPublicKey()
    {
        StringBuilder builder = new();
        _ = builder.AppendLine("-----BEGIN RSA PUBLIC KEY-----");
        var publicKeyBytes = Convert.ToBase64String(((RSA)_key).ExportRSAPublicKey()).ToCharArray();
        for (var i = 0; i < publicKeyBytes.Length; i += 64)
            _ = builder.AppendLine(new string(publicKeyBytes, i, Math.Min(64, publicKeyBytes.Length - i)));
        _ = builder.AppendLine("-----END RSA PUBLIC KEY-----");
        return builder.ToString();
    }

    /// <summary>
    ///     Returns the SSH public key in PuTTY format
    /// </summary>
    /// <returns></returns>
    public override string ToPuTTYPublicKey()
    {
        var sshrsaBytes = Encoding.Default.GetBytes("ssh-rsa");
        var parameters = ((RSA)_key).ExportParameters(false);
        var n = parameters.Modulus;
        var e = parameters.Exponent;
        string publicBase64Key;
        using (MemoryStream ms = new())
        {
            ms.Write(ToBytes(sshrsaBytes.Length), 0, 4);
            ms.Write(sshrsaBytes, 0, sshrsaBytes.Length);

            ms.Write(ToBytes(e.Length), 0, 4);
            ms.Write(e, 0, e.Length);

            ms.Write(ToBytes(n.Length + 1), 0, 4);
            ms.Write(new byte[] { 0 }, 0, 1);
            ms.Write(n, 0, n.Length);

            ms.Flush();
            publicBase64Key = Convert.ToBase64String(ms.ToArray());
        }

        return $"ssh-rsa {publicBase64Key} {_publicComment}";
    }

    #endregion
}