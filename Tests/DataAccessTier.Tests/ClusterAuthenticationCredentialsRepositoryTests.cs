using System.Net;
using System.Text;

using FluentAssertions;

using HEAppE.DataAccessTier;
using HEAppE.DataAccessTier.UnitOfWork;
using HEAppE.DomainObjects.ClusterInformation;

using Testcontainers.MsSql;
namespace DataAccessTier.Tests;

public class ClusterAuthenticationCredentialsRepositoryTests : IAsyncLifetime
{

    private const string _vaultPath = @"v1/HEAppE/data/ClusterAuthenticationCredentials";
    private MsSqlContainer _dbContainer = new MsSqlBuilder()
           .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
           .WithPassword("Strong_password_123!")
           .WithPortBinding(1433, true)
           .WithAutoRemove(true)
           .Build();


    private ClusterAuthenticationCredentials TestEntity = new HEAppE.DomainObjects.ClusterInformation.ClusterAuthenticationCredentials()
    {
        AuthenticationType = HEAppE.DomainObjects.ClusterInformation.ClusterAuthenticationCredentialsAuthType.PrivateKeyInVaultAndInSshAgent,
        CipherType = HEAppE.DomainObjects.FileTransfer.FileTransferCipherType.Unknown,
        IsGenerated = true,
        Password = "password1",
        PrivateKeyCertificate = "cert",
        PrivateKey =
        """
        -----BEGIN RSA PRIVATE KEY-----
        Proc-Type: 4,ENCRYPTED
        DEK-Info: AES-128-CBC,8FE30E57B0E5816DB0D70414BCB63AB8

        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
        -----END RSA PRIVATE KEY-----
        """,
        IsDeleted = false,
        PrivateKeyPassphrase = "eIS9C8YxqfQN4c2jacA3dQ==",
        PublicKeyFingerprint = "63e290829894f6ef6c560e13264e6646c833b2311c8b9d25d7d570d9010ae626",
        Username = "username1"
    };


    // run compose init and add connection to proxy

    [Fact]
    public async Task Insert_New_Entity_Should_Assign_New_Id_And_Write_To_Vault()
    {
        // Arrange

        IUnitOfWork uow = new DatabaseUnitOfWork();
        var preparedEntityId = TestEntity.Id;

        // Act

        uow.ClusterAuthenticationCredentialsRepository.Insert(TestEntity);
        uow.Save();

        using var httpClient = new HttpClient();
        httpClient.BaseAddress = new Uri("http://localhost:8100");
        var path = $"{_vaultPath}/{TestEntity.Id}";
        var result = await httpClient.GetStringAsync(path);
        var vaultPart = ClusterProjectCredentialVaultPart.FromVaultJsonData(result);
        //decode private key
        vaultPart = vaultPart with { PrivateKey = Encoding.UTF8.GetString(Convert.FromBase64String(vaultPart.PrivateKey)).Replace("\n", "\r\n") };
        // Assert

        TestEntity.Id.Should().NotBe(preparedEntityId);
        var xx = TestEntity.ExportVaultData(false);
        TestEntity.ExportVaultData(false).Should().BeEquivalentTo(vaultPart);
    }
    [Fact]
    public async Task Read_Entity_With_Id_11_Should_have_Valid_Private_Key()
    {
        // Arrange

        IUnitOfWork uow = new DatabaseUnitOfWork();
        int preparedEntityId = 11;

        // Act

        var entity = uow.ClusterAuthenticationCredentialsRepository.GetById(preparedEntityId);

        // Assert

        entity.Id.Should().Be(preparedEntityId);
        entity.ExportVaultData().Id.Should().Be(preparedEntityId);
    }


    [Theory()]
    [InlineData(1)]
    public async Task Read_Entity_Should_Return_With_Data_From_Vault(int preparedEntityId)
    {
        // Arrange

        IUnitOfWork uow = new DatabaseUnitOfWork();

        // Act

        var entity = uow.ClusterAuthenticationCredentialsRepository.GetById(preparedEntityId);

        // Assert

        entity.Id.Should().Be(preparedEntityId);
        entity.ExportVaultData().Id.Should().Be(preparedEntityId);
    }


    [Fact]
    public async Task Set_Vault_Variable_Should_Return_Ok()
    {
        // Arrange

        using var httpClient = new HttpClient();
        httpClient.BaseAddress = new Uri("http://localhost:8100");
        var path = $"{_vaultPath}/{TestEntity.Id}";
        var contentpart = TestEntity.ExportVaultData();
        var content = contentpart.AsVaultDataJsonObject();
        var payload = new StringContent(content, Encoding.UTF8, "application/json");

        // Act

        var result = await httpClient.PostAsync(path, payload);

        // Assert

        result.Should().HaveStatusCode(HttpStatusCode.OK);
    }

    [Theory()]
    [InlineData(1)]
    public async Task Get_Vault_Variable_Should_Return_Ok(int entityId)
    {
        // Arrange

        using var httpClient = new HttpClient();
        httpClient.BaseAddress = new Uri("http://localhost:8100");
        var path = $"{_vaultPath}/{entityId}";

        // Act
        var result = await httpClient.GetAsync(path);

        // Assert

        result.Should().HaveStatusCode(HttpStatusCode.OK);
    }



    public async Task InitializeAsync()
    {
        await _dbContainer.StartAsync();
        MiddlewareContextSettings.ConnectionString = _dbContainer.GetConnectionString();

        IUnitOfWork uow = new DatabaseUnitOfWork();
        SeedDBDataForTest(uow);
    }


    private void SeedDBDataForTest(IUnitOfWork uow)
    {

        for (int i = 1; i < 11; i++)
        {
            uow.ClusterAuthenticationCredentialsRepository.Insert(new ClusterAuthenticationCredentials()
            {
                AuthenticationType = ClusterAuthenticationCredentialsAuthType.Password,
                CipherType = HEAppE.DomainObjects.FileTransfer.FileTransferCipherType.Unknown,
                IsGenerated = false,
                Password = $"seed{i}",
                PrivateKey = $"seed{i}",
                IsDeleted = false,
                PrivateKeyPassphrase = $"seed{i}",
                PublicKeyFingerprint = null,
                Username = $"seed{i}"
            });
            uow.Save();

        }
        uow.ClusterAuthenticationCredentialsRepository.Insert(TestEntity);
        uow.Save();

    }

    public Task DisposeAsync() => _dbContainer.StopAsync();

}