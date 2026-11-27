using HEAppE.DomainObjects.ClusterInformation;
using HEAppE.DomainObjects.JobManagement;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Xunit;
using FluentAssertions;

namespace HEAppE.DataAccessTier.Tests;

[Trait("Category", "Unit")]
public class ClusterProjectPreferredAuthTypeTests
{
    [Fact]
    public void ClusterProject_PreferredAuthType_SentinelIsNotUnknown_SoZeroIsExplicitlyInserted()
    {
        var options = new DbContextOptionsBuilder<MiddlewareContext>()
            .UseSqlServer("Server=localhost;Database=dummy;TrustServerCertificate=true;")
            .Options;

        using var context = new MiddlewareContext(options);
        var entityType = context.Model.FindEntityType(typeof(ClusterProject));
        entityType.Should().NotBeNull();

        var property = entityType!.FindProperty(nameof(ClusterProject.PreferredAuthType));
        property.Should().NotBeNull();

        property!.GetDefaultValue().Should().Be(ClusterAuthenticationCredentialsAuthType.PrivateKey);
        property.Sentinel.Should().NotBe(ClusterAuthenticationCredentialsAuthType.Unknown);
        property.Sentinel.Should().Be((ClusterAuthenticationCredentialsAuthType)(-1));
    }

    [Theory]
    [InlineData(ClusterAuthenticationCredentialsAuthType.Unknown)]
    [InlineData(ClusterAuthenticationCredentialsAuthType.Password)]
    [InlineData(ClusterAuthenticationCredentialsAuthType.PrivateKey)]
    [InlineData(ClusterAuthenticationCredentialsAuthType.PrivateKeyInVaultAndInSshAgent)]
    [InlineData(ClusterAuthenticationCredentialsAuthType.Kerberos)]
    public void ClusterProject_PreferredAuthType_PersistsRequestedValue(ClusterAuthenticationCredentialsAuthType authType)
    {
        var options = new DbContextOptionsBuilder<MiddlewareContext>()
            .UseInMemoryDatabase($"Test_ClusterProject_AuthType_{authType}_{System.Guid.NewGuid():N}")
            .Options;

        using (var context = new MiddlewareContext(options))
        {
            context.ClusterProjects.Add(new ClusterProject
            {
                Id = 1,
                ProjectId = 10,
                ClusterId = 20,
                ScratchStoragePath = "/scratch",
                ProjectStoragePath = "/project",
                PreferredAuthType = authType
            });
            context.SaveChanges();
        }

        using (var context = new MiddlewareContext(options))
        {
            var fetched = context.ClusterProjects.Find(1L);
            fetched.Should().NotBeNull();
            fetched!.PreferredAuthType.Should().Be(authType);
        }
    }
}
