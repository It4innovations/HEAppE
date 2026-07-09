using System.Collections.Generic;
using HEAppE.Utils;
using SshCaAPI.Configuration;
using Xunit;

namespace SshCaAPITests
{
    public class ClusterContextSettingsTests
    {
        [Fact]
        public void SshCaSettings_ShouldCorrectlyResolveOverriddenValuesWithinClusterContext()
        {
            // Arrange
            SshCaSettings.UseCertificateAuthorityForAuthentication = false;
            SshCaSettings.BaseUri = "http://localhost-global";

            var customConfig = new Dictionary<string, string>
            {
                { "SshCaSettings:UseCertificateAuthorityForAuthentication", "true" },
                { "SshCaSettings:BaseUri", "http://cluster-specific-uri" }
            };

            // Assert default values outside context
            Assert.False(SshCaSettings.UseCertificateAuthorityForAuthentication);
            Assert.Equal("http://localhost-global", SshCaSettings.BaseUri);

            // Act & Assert inside context
            using (ClusterContext.Use(customConfig))
            {
                Assert.True(SshCaSettings.UseCertificateAuthorityForAuthentication);
                Assert.Equal("http://cluster-specific-uri", SshCaSettings.BaseUri);
            }

            // Assert restored values after context disposal
            Assert.False(SshCaSettings.UseCertificateAuthorityForAuthentication);
            Assert.Equal("http://localhost-global", SshCaSettings.BaseUri);
        }

        [Fact]
        public void GetCredentialsAuthenticationType_ShouldRespectClusterSpecificSshCaOverride()
        {
            // Arrange
            SshCaSettings.UseCertificateAuthorityForAuthentication = true;
            
            var credential = new HEAppE.DomainObjects.ClusterInformation.ClusterAuthenticationCredentials
            {
                PrivateKey = "some-key"
            };

            var clusterWithOverride = new HEAppE.DomainObjects.ClusterInformation.Cluster
            {
                CustomConfiguration = new Dictionary<string, string>
                {
                    { "SshCaSettings:UseCertificateAuthorityForAuthentication", "false" }
                }
            };

            var clusterWithoutOverride = new HEAppE.DomainObjects.ClusterInformation.Cluster
            {
                CustomConfiguration = new Dictionary<string, string>()
            };

            // Act
            var typeWithOverride = ClusterAuthenticationCredentialsUtils.GetCredentialsAuthenticationType(credential, clusterWithOverride);
            var typeWithoutOverride = ClusterAuthenticationCredentialsUtils.GetCredentialsAuthenticationType(credential, clusterWithoutOverride);

            // Assert
            Assert.Equal(HEAppE.DomainObjects.ClusterInformation.ClusterAuthenticationCredentialsAuthType.PrivateKey, typeWithOverride);
            Assert.Equal(HEAppE.DomainObjects.ClusterInformation.ClusterAuthenticationCredentialsAuthType.SshCertificate, typeWithoutOverride);
        }
    }
}
