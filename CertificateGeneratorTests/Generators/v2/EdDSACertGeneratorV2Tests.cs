using HEAppE.CertificateGenerator.Generators.v2;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.OpenSsl;

namespace CertificateGeneratorTests.Generators.v2;

public class EdDSACertGeneratorV2Tests
{
    private readonly EdDSACertGeneratorV2 _generator;

    public EdDSACertGeneratorV2Tests()
    {
        _generator = new EdDSACertGeneratorV2();
    }

    #region ToPublicKeyInAuthorizedKeysFormat

    [Fact]
    public void ToPublicKeyInAuthorizedKeysFormat_Should_Return_PublicKey_With_No_Comment()
    {
        // Assign

        // Act
        var publicKey = _generator.ToPublicKeyInAuthorizedKeysFormat();

        // Assert
        Assert.NotNull(publicKey);
        Assert.StartsWith("ssh-ed25519", publicKey);
    }

    [Fact]
    public void ToPublicKeyInAuthorizedKeysFormat_Should_Return_PublicKey_With_Comment()
    {
        // Assign
        var comment = "testComment";

        // Act
        var publicKey = _generator.ToPublicKeyInAuthorizedKeysFormat(comment);

        // Assert
        Assert.NotNull(publicKey);
        Assert.StartsWith("ssh-ed25519", publicKey);
        Assert.EndsWith(comment, publicKey);
    }

    #endregion

    #region GetPublicKeyFingerprint

    [Fact]
    public void GetPublicKeyFingerprint_Should_Return_PublicKeyFingerprint()
    {
        // Assign

        // Act
        var fingerprint = _generator.GetPublicKeyFingerprint();

        // Assert
        Assert.NotNull(fingerprint);
    }

    #endregion

    #region ToEncryptedPrivateKeyInPEM

    [Fact]
    public void ToEncryptedPrivateKeyInPEM_Should_Return_EncryptedPrivateKey_In_PEM_Format()
    {
        // Assign
        var password = "testPassword";

        // Act
        var encryptedKey = _generator.ToEncryptedPrivateKeyInPEM(password);

        // Assert
        Assert.NotNull(encryptedKey);
        Assert.StartsWith("-----BEGIN PRIVATE KEY-----", encryptedKey);
        Assert.EndsWith("-----END PRIVATE KEY-----\n", encryptedKey.Replace("\r\n", "\n"));
        Assert.Contains("Proc-Type: 4,ENCRYPTED", encryptedKey);
        Assert.Contains("DEK-Info: AES-256-CBC", encryptedKey);
        // Try decipher
        using var reader = new StringReader(encryptedKey);
        var pemReader = new PemReader(reader, new PasswordFinder(password));
        var decryptedObject = pemReader.ReadObject();
        Assert.NotNull(decryptedObject);
        Assert.IsAssignableFrom<Ed25519PrivateKeyParameters>(decryptedObject);
    }

    #endregion

    #region ToPrivateKeyInPEM

    [Fact]
    public void ToPrivateKeyInPEM_Should_Return_PrivateKey_In_PEM_Format()
    {
        // Assign

        // Act
        var privateKey = _generator.ToPrivateKeyInPEM();

        // Assert
        Assert.NotNull(privateKey);
        Assert.StartsWith("-----BEGIN PRIVATE KEY-----", privateKey);
        Assert.EndsWith("-----END PRIVATE KEY-----\n", privateKey.Replace("\r\n", "\n"));
        // Try decipher
        using var reader = new StringReader(privateKey);
        var pemReader = new PemReader(reader);
        var decryptedObject = pemReader.ReadObject();
        Assert.NotNull(decryptedObject);
        Assert.IsAssignableFrom<Ed25519PrivateKeyParameters>(decryptedObject);
    }

    #endregion

    #region ToPublicKeyInAuthorizedKeysFormatFromPrivateKey

    [Fact]
    public void ToPublicKeyInAuthorizedKeysFormatFromPrivateKey__Should_Return_PublicKey_With_No_Comment()
    {
        // Assign
        var password = "testPassword";
        var publicKeyOriginal = _generator.ToPublicKeyInAuthorizedKeysFormat();
        var encryptedKey = _generator.ToEncryptedPrivateKeyInPEM(password);

        // Act
        var publicKey = EdDSACertGeneratorV2.ToPublicKeyInAuthorizedKeysFormatFromPrivateKey(encryptedKey, password);

        // Assert
        Assert.NotNull(publicKey);
        Assert.StartsWith("ssh-ed25519", publicKey);
        Assert.Equal(publicKeyOriginal, publicKey);
    }

    [Fact]
    public void ToPublicKeyInAuthorizedKeysFormatFromPrivateKey__Should_Return_PublicKey_With_Comment()
    {
        // Assign
        var password = "testPassword";
        var comment = "testComment";
        var publicKeyOriginal = _generator.ToPublicKeyInAuthorizedKeysFormat(comment);
        var encryptedKey = _generator.ToEncryptedPrivateKeyInPEM(password);

        // Act
        var publicKey = EdDSACertGeneratorV2.ToPublicKeyInAuthorizedKeysFormatFromPrivateKey(encryptedKey, password, comment);

        // Assert
        Assert.NotNull(publicKey);
        Assert.StartsWith("ssh-ed25519", publicKey);
        Assert.EndsWith(comment, publicKey);
        Assert.Equal(publicKeyOriginal, publicKey);
    }

    #endregion

    #region ToPublicKeyInPEM

    [Fact]
    public void ToPublicKeyInPEM_Should_Return_PublicKey_In_PEM_Format()
    {
        // Assign

        // Act
        var publicKey = _generator.ToPublicKeyInPEM();

        // Assert
        Assert.NotNull(publicKey);
        Assert.StartsWith("-----BEGIN OPENSSH PUBLIC KEY-----", publicKey);
        Assert.EndsWith("-----END OPENSSH PUBLIC KEY-----\n", publicKey.Replace("\r\n", "\n"));
    }

    #endregion

    #region ToPublicKeyInPEMFromPrivateKey

    [Fact]
    public void ToPublicKeyInPEMFromPrivateKey_Should_Return_PublicKey()
    {
        // Assign
        var password = "testPassword";
        var publicKeyOriginal = _generator.ToPublicKeyInPEM();
        var encryptedKey = _generator.ToEncryptedPrivateKeyInPEM(password);

        // Act
        var publicKey = EdDSACertGeneratorV2.ToPublicKeyInPEMFromPrivateKey(encryptedKey, password);

        // Assert
        Assert.NotNull(publicKey);
        Assert.StartsWith("-----BEGIN OPENSSH PUBLIC KEY-----", publicKey);
        Assert.EndsWith("-----END OPENSSH PUBLIC KEY-----\n", publicKey.Replace("\r\n", "\n"));
        Assert.Equal(publicKeyOriginal, publicKey);
    }

    #endregion

    #region Regenerate

    [Fact]
    public void Regenerate_Should_Return_Different_Keys()
    {
        // Assign
        var password = "testPassword";
        var publicKeyOriginal = _generator.ToPublicKeyInPEM();
        var encryptedKeyOriginal = _generator.ToEncryptedPrivateKeyInPEM(password);

        // Act
        _generator.Regenerate();

        // Assert
        var publicKeyNew = _generator.ToPublicKeyInPEM();
        Assert.NotEqual(publicKeyOriginal, publicKeyNew);
        var encryptedKeyNew = _generator.ToEncryptedPrivateKeyInPEM(password);
        Assert.NotEqual(encryptedKeyOriginal, encryptedKeyNew);
    }

    #endregion
}
