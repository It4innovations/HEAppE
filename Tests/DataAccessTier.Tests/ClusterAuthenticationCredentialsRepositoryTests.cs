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
        AuthenticationType = HEAppE.DomainObjects.ClusterInformation.ClusterAuthenticationCredentialsAuthType.Password,
        CipherType = HEAppE.DomainObjects.FileTransfer.FileTransferCipherType.Unknown,
        IsGenerated = false,
        Password = "password",
        PrivateKey = "privateKey",
        IsDeleted = false,
        PrivateKeyPassphrase = "password",
        PublicKeyFingerprint = null,
        Username = "username"
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

        // Assert

        TestEntity.Id.Should().NotBe(preparedEntityId);
        TestEntity.ExportVaultData().Should().BeEquivalentTo(vaultPart);
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

    }

    public Task DisposeAsync() => _dbContainer.StopAsync();

}