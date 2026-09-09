using Microsoft.Extensions.Configuration;
using SshCaAPI;
using SshCaAPI.Configuration;

namespace SshCaAPITests
{
    public class SshCertificateAuthorityServiceTests
    {
        ISshCertificateAuthorityService _sshCaService;

        public SshCertificateAuthorityServiceTests()
        {
            // bind SSH CA service configuration
            IConfiguration config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
                .Build();
            // initialize service
            if (!string.IsNullOrEmpty(SshCaSettings.Token))
            {
                _sshCaService = new SshCertificateAuthorityService(SshCaSettings.BaseUri, SshCaSettings.CAName, SshCaSettings.ConnectionTimeoutInSeconds);
            }
        }

        [Fact]
        public async Task GetConfigAsync_should_return_publicKey()
        {
            if (_sshCaService == null) return;

            // Act
            var configResult = await _sshCaService.GetConfigAsync();

            // Assert
            Assert.NotNull(configResult);
            Assert.NotNull(configResult.PublicKey);
        }

        [Fact]
        public async Task SignAsync_should_return_privateKey()
        {
            if (_sshCaService == null) return;

            // Act
            var configResult = await _sshCaService.GetConfigAsync();
            var signResult = await _sshCaService.SignAsync(configResult.PublicKey, SshCaSettings.Token!, "localhost", null);

            // Assert
            Assert.NotNull(signResult);
        }
    }
}
