using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto.Utilities;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.Utilities.IO.Pem;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using PemWriter = Org.BouncyCastle.OpenSsl.PemWriter;

namespace HEAppE.CertificateGenerator.Generators.v2;

public class EdDSACertGeneratorV2 : GenericCertGeneratorV2
{
    /// <summary>
    ///     Initializes a new instance of the EdDSACertGeneratorV2 class with a default key size of 4096 bits.
    /// </summary>
    public EdDSACertGeneratorV2()
    {
        _publicComment = $"generated@{Environment.MachineName}";
        _generator = new Ed25519KeyPairGenerator();
        _generator.Init(new Ed25519KeyGenerationParameters(new SecureRandom()));
        _keyPair = _generator.GenerateKeyPair();
    }

    /// <summary>
    ///     Regenerates the RSA key pair.
    /// </summary>
    public override void Regenerate()
    {
        _keyPair = _generator.GenerateKeyPair();
    }

    /// <summary>
    ///     Converts the public key to the authorized_keys format.
    /// </summary>
    /// <returns></returns>
    public override string ToPublicKeyInAuthorizedKeysFormat(string comment = null)
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
        var suffix = !string.IsNullOrEmpty(comment) ? comment : _publicComment;
        return $"ssh-ed25519 {base64} {suffix}";
    }

    /// <summary>
    ///     Returns Public Key Fingerprint in the SHA256 algorithm.
    /// </summary>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public override string GetPublicKeyFingerprint()
    {
        var pubBlob = OpenSshPublicKeyUtilities.EncodePublicKey((AsymmetricKeyParameter)_keyPair.Public);
        var hash = DigestUtilities.CalculateDigest("SHA-256", pubBlob);
        var b64 = Convert.ToBase64String(hash).TrimEnd('=');
        return $"SHA256:{b64}";
    }

    /// <summary>
    ///     Converts the private key to an encrypted PEM format.
    /// </summary>
    /// <param name="passphrase">The passphrase to encrypt the private key.</param>
    /// <param name="cipherAlgorithm">The cipher algorithm to use for encryption (default: AES-256-CBC).</param>
    /// <returns>The private key in encrypted PEM format.</returns>
    public override string ToEncryptedPrivateKeyInPEM(string passphrase, string cipherAlgorithm = "AES-256-CBC")
    {
        using var stringWriter = new StringWriter();
        using var pemWriter = new PemWriter(stringWriter);
        var privateKey = _keyPair.Private as Ed25519PrivateKeyParameters;
        pemWriter.WriteObject(privateKey, cipherAlgorithm, passphrase.ToCharArray(), new SecureRandom());
        pemWriter.Writer.Flush();
        return stringWriter.ToString();
    }

    /// <summary>
    ///     Converts the private key to PEM format.
    /// </summary>
    /// <returns>The private key in PEM format.</returns>
    public override string ToPrivateKeyInPEM()
    {
        var builder = new StringBuilder();
        builder.AppendLine("-----BEGIN OPENSSH PRIVATE KEY-----");
        var privBytes = OpenSshPrivateKeyUtilities.EncodePrivateKey((AsymmetricKeyParameter)_keyPair.Private);
        builder.AppendLine(Convert.ToBase64String(privBytes, Base64FormattingOptions.InsertLineBreaks));
        builder.AppendLine("-----END OPENSSH PRIVATE KEY-----");
        return builder.ToString();
    }

    /// <summary>
    ///     Converts the private key to 
    /// </summary>
    /// <param name="privateKeyPem"></param>
    /// <param name="passphrase"></param>
    /// <param name="comment"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public static string ToPublicKeyInAuthorizedKeysFormatFromPrivateKey(string privateKeyPem, string passphrase,
        string comment = null)
    {
        var b64 = string.Join("",
            privateKeyPem
              .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
              .Where(l => !l.StartsWith("-----BEGIN") && !l.StartsWith("-----END"))
        );

        var data = Convert.FromBase64String(b64);
        int idx = 0;

        var magic = Encoding.ASCII.GetBytes("openssh-key-v1\0");
        if (data.Length < magic.Length ||
            !data.Take(magic.Length).SequenceEqual(magic))
        {
            throw new ArgumentException("Not an OpenSSH private key.");
        }
        idx += magic.Length;

        var cipherName = ReadString(data, ref idx);
        var kdfName = ReadString(data, ref idx);
        var kdfOptions = ReadBytes(data, ref idx);

        var nkeys = ReadUInt32(data, ref idx);
        if (nkeys < 1)
            throw new ArgumentException("OpenSSH key neobsahuje žiadny public key.");

        var pubBlob = ReadBytes(data, ref idx);

        var pubB64 = Convert.ToBase64String(pubBlob);
        var suf = !string.IsNullOrEmpty(comment)
                     ? comment
                     : _publicComment;
        return $"ssh-ed25519 {pubB64} {suf}";
    }

    /// <summary>
    ///     Converts the public key to PEM format.
    /// </summary>
    /// <returns>The public key in PEM format.</returns>
    public override string ToPublicKeyInPEM()
    {
        using var sw = new StringWriter();
        using var pw = new PemWriter(sw);
        var pubParam = (Ed25519PublicKeyParameters)_keyPair.Public;
        // BouncyCastle PemWriter doesnt include OpenSSH public PEM, so write manually
        var keyType = Encoding.ASCII.GetBytes("ssh-ed25519");
        var raw = pubParam.GetEncoded();
        using var ms = new MemoryStream();
        using (var bw = new BinaryWriter(ms))
        {
            bw.Write(IPAddress.HostToNetworkOrder(keyType.Length));
            bw.Write(keyType);
            bw.Write(IPAddress.HostToNetworkOrder(raw.Length));
            bw.Write(raw);
        }
        var blob = ms.ToArray();
        pw.WriteObject(new PemObject("OPENSSH PUBLIC KEY", blob));
        return sw.ToString();
    }

    static uint ReadUInt32(byte[] data, ref int idx)
    {
        if (idx + 4 > data.Length) throw new ArgumentException("Corrupt OpenSSH key.");
        uint val = ((uint)data[idx] << 24)
                 | ((uint)data[idx + 1] << 16)
                 | ((uint)data[idx + 2] << 8)
                 | (uint)data[idx + 3];
        idx += 4;
        return val;
    }

    static byte[] ReadBytes(byte[] data, ref int idx)
    {
        var len = (int)ReadUInt32(data, ref idx);
        if (idx + len > data.Length) throw new ArgumentException("Corrupt OpenSSH key.");
        var buf = new byte[len];
        Array.Copy(data, idx, buf, 0, len);
        idx += len;
        return buf;
    }

    static string ReadString(byte[] data, ref int idx)
    {
        var buf = ReadBytes(data, ref idx);
        return Encoding.ASCII.GetString(buf);
    }
}
