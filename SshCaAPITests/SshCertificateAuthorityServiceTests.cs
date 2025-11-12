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
            config.Bind("SshCaSettings", new SshCaSettings());

            if (string.IsNullOrEmpty(SshCaSettings.Token))
                throw new ArgumentException("Property Token in SshCaSettings cannot be null");

            // initialize service
            _sshCaService = null;
        }

        [Fact]
        public async Task GetConfigAsync_should_return_publicKey()
        {
            // Assign

            // Act
            var configResult = await _sshCaService.GetConfigAsync();

            // Assert
            Assert.NotNull(configResult);
            Assert.NotNull(configResult.PublicKey);
        }

        [Fact]
        public async Task SignAsync_should_return_privateKey()
        {
            // Assign
            var configResult = await _sshCaService.GetConfigAsync();

            // Act
            var signResult = await _sshCaService.SignAsync(configResult.PublicKey, SshCaSettings.Token!);

            // Assert
            Assert.NotNull(signResult);
        }
    }
}
