using HEAppE.CertificateGenerator.Generators;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.OpenSsl;

namespace CertificateGeneratorTests.Generators;

public class ECDsaCertGeneratorTests
{
    private readonly ECDsaCertGenerator _generator;

    public ECDsaCertGeneratorTests()
    {
        _generator = new ECDsaCertGenerator();
    }

    #region ToPrivateKey

    [Fact]
    public void ToPrivateKey_Should_Return_PrivateKey()
    {
        // Assign

        // Act
        var privateKey = _generator.ToPrivateKey();

        // Assert
        Assert.NotNull(privateKey);
        Assert.StartsWith("-----BEGIN EC PRIVATE KEY-----", privateKey);
        Assert.EndsWith("-----END EC PRIVATE KEY-----\n", privateKey.Replace("\r\n", "\n"));
        // Try decipher
        using var reader = new StringReader(privateKey);
        var pemReader = new PemReader(reader);
        var decryptedObject = pemReader.ReadObject();
        Assert.NotNull(decryptedObject);
        Assert.IsAssignableFrom<AsymmetricCipherKeyPair>(decryptedObject);
    }

    #endregion

    #region ToPublicKey

    [Fact]
    public void ToPublicKey_Should_Return_PublicKey()
    {
        // Assign

        // Act
        var publicKey = _generator.ToPublicKey();

        // Assert
        Assert.NotNull(publicKey);
        Assert.StartsWith("-----BEGIN PUBLIC KEY-----", publicKey);
        Assert.EndsWith("-----END PUBLIC KEY-----\n", publicKey.Replace("\r\n", "\n"));
        // Try decipher
        using var reader = new StringReader(publicKey);
        var pemReader = new PemReader(reader);
        var decryptedObject = pemReader.ReadObject();
        Assert.NotNull(decryptedObject);
        Assert.IsAssignableFrom<ECPublicKeyParameters>(decryptedObject);
    }

    #endregion

    #region ToPuTTYPublicKey

    [Fact]
    public void ToPuTTYPublicKey_Should_Return_PuTTYPublicKey_Without_Comment()
    {
        // Assign

        // Act
        var publicKey = _generator.ToPuTTYPublicKey();

        // Assert
        Assert.NotNull(publicKey);
        Assert.StartsWith("ecdsa-sha2-nistp", publicKey);
    }

    [Fact]
    public void ToPuTTYPublicKey_Should_Return_PuTTYPublicKey_With_Comment()
    {
        // Assign
        var comment = "testComment";
        var generator = new ECDsaCertGenerator("nistP256", comment);

        // Act
        var publicKey = generator.ToPuTTYPublicKey();

        // Assert
        Assert.NotNull(publicKey);
        Assert.StartsWith("ecdsa-sha2-nistp", publicKey);
        Assert.EndsWith(comment, publicKey);
    }

    #endregion

    #region Regenerate

    [Fact]
    public void Regenerate_Should_Return_Different_Keys()
    {
        // Assign
        var publicKeyOriginal = _generator.ToPublicKey();
        var privateKeyOriginal = _generator.ToPrivateKey();

        // Act
        _generator.Regenerate();

        // Assert
        var publicKeyNew = _generator.ToPublicKey();
        Assert.NotEqual(publicKeyOriginal, publicKeyNew);
        var privateKeyNew = _generator.ToPrivateKey();
        Assert.NotEqual(privateKeyOriginal, privateKeyNew);
    }

    #endregion
}
