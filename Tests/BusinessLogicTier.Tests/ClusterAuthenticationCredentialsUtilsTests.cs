using HEAppE.DomainObjects.ClusterInformation;
using HEAppE.Utils;
using SshCaAPI.Configuration;
using FluentAssertions;
using Xunit;

namespace HEAppE.BusinessLogicTier.Tests;

[Trait("Category", "Unit")]
public class ClusterAuthenticationCredentialsUtilsTests
{
    [Fact]
    public void GetCredentialsAuthenticationType_WhenPrivateKeyIsPresent_ShouldNotReturnSshAgent()
    {
        var cred = new ClusterAuthenticationCredentials
        {
            Username = "testuser",
            PrivateKey = "-----BEGIN OPENSSH PRIVATE KEY-----\ntest\n-----END OPENSSH PRIVATE KEY-----",
            AuthenticationType = ClusterAuthenticationCredentialsAuthType.PrivateKeyInSshAgent
        };

        var cluster = new Cluster
        {
            ConnectionProtocol = ClusterConnectionProtocol.Ssh
        };

        var result = ClusterAuthenticationCredentialsUtils.GetCredentialsAuthenticationType(cred, cluster);

        result.Should().NotBe(ClusterAuthenticationCredentialsAuthType.PrivateKeyInSshAgent);
        result.Should().Be(SshCaSettings.UseCertificateAuthorityForAuthentication
            ? ClusterAuthenticationCredentialsAuthType.SshCertificate
            : ClusterAuthenticationCredentialsAuthType.PrivateKey);
    }

    [Fact]
    public void GetCredentialsAuthenticationType_WhenPrivateKeyIsPresentWithProxy_ShouldReturnProxyKeyOrCert()
    {
        var cred = new ClusterAuthenticationCredentials
        {
            Username = "testuser",
            PrivateKey = "-----BEGIN OPENSSH PRIVATE KEY-----\ntest\n-----END OPENSSH PRIVATE KEY-----",
            AuthenticationType = ClusterAuthenticationCredentialsAuthType.PrivateKeyInSshAgent
        };

        var cluster = new Cluster
        {
            ConnectionProtocol = ClusterConnectionProtocol.Ssh,
            ProxyConnection = new ClusterProxyConnection { Host = "proxy.example.com", Port = 22 }
        };

        var result = ClusterAuthenticationCredentialsUtils.GetCredentialsAuthenticationType(cred, cluster);

        result.Should().NotBe(ClusterAuthenticationCredentialsAuthType.PrivateKeyInSshAgent);
        result.Should().Be(SshCaSettings.UseCertificateAuthorityForAuthentication
            ? ClusterAuthenticationCredentialsAuthType.SshCertificateViaProxy
            : ClusterAuthenticationCredentialsAuthType.PrivateKeyViaProxy);
    }

    [Fact]
    public void GetCredentialsAuthenticationType_WhenAlreadyPrivateKey_ShouldPreserveEvenIfPrivateKeyNull()
    {
        var cred = new ClusterAuthenticationCredentials
        {
            Username = "testuser",
            PrivateKey = null,
            AuthenticationType = ClusterAuthenticationCredentialsAuthType.PrivateKey
        };

        var cluster = new Cluster
        {
            ConnectionProtocol = ClusterConnectionProtocol.Ssh
        };

        var result = ClusterAuthenticationCredentialsUtils.GetCredentialsAuthenticationType(cred, cluster);

        result.Should().NotBe(ClusterAuthenticationCredentialsAuthType.PrivateKeyInSshAgent);
        result.Should().Be(SshCaSettings.UseCertificateAuthorityForAuthentication
            ? ClusterAuthenticationCredentialsAuthType.SshCertificate
            : ClusterAuthenticationCredentialsAuthType.PrivateKey);
    }

    [Fact]
    public void GetCredentialsAuthenticationType_WhenAlreadySshCertificate_ShouldPreserveEvenIfPrivateKeyNull()
    {
        var cred = new ClusterAuthenticationCredentials
        {
            Username = "testuser",
            PrivateKey = null,
            AuthenticationType = ClusterAuthenticationCredentialsAuthType.SshCertificate
        };

        var cluster = new Cluster
        {
            ConnectionProtocol = ClusterConnectionProtocol.Ssh
        };

        var result = ClusterAuthenticationCredentialsUtils.GetCredentialsAuthenticationType(cred, cluster);

        result.Should().Be(ClusterAuthenticationCredentialsAuthType.SshCertificate);
    }

    [Fact]
    public void GetCredentialsAuthenticationType_WhenUnknownAndNoKeys_ShouldDefaultToPrivateKeyNotSshAgent()
    {
        var cred = new ClusterAuthenticationCredentials
        {
            Username = "testuser",
            PrivateKey = null,
            Password = null,
            AuthenticationType = ClusterAuthenticationCredentialsAuthType.Unknown
        };

        var cluster = new Cluster
        {
            ConnectionProtocol = ClusterConnectionProtocol.Ssh
        };

        var result = ClusterAuthenticationCredentialsUtils.GetCredentialsAuthenticationType(cred, cluster);

        result.Should().NotBe(ClusterAuthenticationCredentialsAuthType.PrivateKeyInSshAgent);
        result.Should().Be(SshCaSettings.UseCertificateAuthorityForAuthentication
            ? ClusterAuthenticationCredentialsAuthType.SshCertificate
            : ClusterAuthenticationCredentialsAuthType.PrivateKey);
    }

    [Fact]
    public void GetCredentialsAuthenticationType_WhenClusterIsNull_ShouldNotThrow()
    {
        var cred = new ClusterAuthenticationCredentials
        {
            Username = "testuser",
            PrivateKey = "-----BEGIN OPENSSH PRIVATE KEY-----\ntest\n-----END OPENSSH PRIVATE KEY-----"
        };

        var act = () => ClusterAuthenticationCredentialsUtils.GetCredentialsAuthenticationType(cred, null);

        act.Should().NotThrow();
    }

    [Fact]
    public void GetCredentialsAuthenticationType_WhenSshCaEnabled_ShouldReturnSshCertificateOrViaProxy()
    {
        var prevSetting = SshCaSettings.UseCertificateAuthorityForAuthentication;
        try
        {
            SshCaSettings.UseCertificateAuthorityForAuthentication = true;

            var credNoKey = new ClusterAuthenticationCredentials
            {
                Username = "testuser"
            };

            var directCluster = new Cluster { ConnectionProtocol = ClusterConnectionProtocol.Ssh };
            var proxyCluster = new Cluster
            {
                ConnectionProtocol = ClusterConnectionProtocol.Ssh,
                ProxyConnection = new ClusterProxyConnection { Host = "proxy.example.com", Port = 22 }
            };

            var directResult = ClusterAuthenticationCredentialsUtils.GetCredentialsAuthenticationType(credNoKey, directCluster);
            var proxyResult = ClusterAuthenticationCredentialsUtils.GetCredentialsAuthenticationType(credNoKey, proxyCluster);

            directResult.Should().Be(ClusterAuthenticationCredentialsAuthType.SshCertificate);
            proxyResult.Should().Be(ClusterAuthenticationCredentialsAuthType.SshCertificateViaProxy);
        }
        finally
        {
            SshCaSettings.UseCertificateAuthorityForAuthentication = prevSetting;
        }
    }

    [Fact]
    public void ComputeFingerprintFromAuthorizedKeys_ForEd25519_ShouldReturnSha256Format()
    {
        var edGenerator = new HEAppE.CertificateGenerator.Generators.v2.EdDSACertGeneratorV2();
        var authorizedKey = edGenerator.ToPublicKeyInAuthorizedKeysFormat("testuser");
        var expectedFingerprint = edGenerator.GetPublicKeyFingerprint();

        var fingerprint = HEAppE.CertificateGenerator.SSHGenerator.ComputeFingerprintFromAuthorizedKeys(authorizedKey);

        fingerprint.Should().NotBeNullOrEmpty();
        fingerprint.Should().StartWith("SHA256:");
        fingerprint.Should().Be(expectedFingerprint);
    }

    [Fact]
    public void ComputeFingerprintFromAuthorizedKeys_ForRSA_ShouldReturnHexFormat()
    {
        var rsaGenerator = new HEAppE.CertificateGenerator.Generators.v2.RSACertGeneratorV2(3072);
        var authorizedKey = rsaGenerator.ToPublicKeyInAuthorizedKeysFormat("testuser");
        var expectedFingerprint = rsaGenerator.GetPublicKeyFingerprint();

        var fingerprint = HEAppE.CertificateGenerator.SSHGenerator.ComputeFingerprintFromAuthorizedKeys(authorizedKey);

        fingerprint.Should().NotBeNullOrEmpty();
        fingerprint.Should().Be(expectedFingerprint);
    }

    [Fact]
    public void GetPublicKeyFromPrivateKey_ForEd25519_ShouldDerivePublicKeyAndFingerprint()
    {
        var edGenerator = new HEAppE.CertificateGenerator.Generators.v2.EdDSACertGeneratorV2();
        var privKeyPem = edGenerator.ToPrivateKeyInPEM();
        var expectedFingerprint = edGenerator.GetPublicKeyFingerprint();

        var cred = new ClusterAuthenticationCredentials
        {
            Username = "testuser",
            PrivateKey = privKeyPem,
            CipherType = HEAppE.DomainObjects.FileTransfer.FileTransferCipherType.Ed25519,
            AuthenticationType = ClusterAuthenticationCredentialsAuthType.PrivateKeyInSshAgent
        };

        var derivedKey = HEAppE.CertificateGenerator.SSHGenerator.GetPublicKeyFromPrivateKey(cred);

        derivedKey.Should().NotBeNull();
        derivedKey.PublicKeyInAuthorizedKeysFormat.Should().StartWith("ssh-ed25519");
        derivedKey.PublicKeyFingerprint.Should().Be(expectedFingerprint);
    }
}
