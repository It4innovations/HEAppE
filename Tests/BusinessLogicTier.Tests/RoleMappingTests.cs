using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using HEAppE.ExternalAuthentication.Configuration;
using Xunit;

namespace HEAppE.BusinessLogicTier.Tests;

[Trait("Category", "Unit")]
public class RoleMappingTests : IDisposable
{
    public RoleMappingTests()
    {
        ResetRoleMapping();
    }

    public void Dispose()
    {
        ResetRoleMapping();
    }

    private static void ResetRoleMapping()
    {
        RoleMapping.ManagementAdmin = null;
        RoleMapping.Maintainer = null;
        RoleMapping.Manager = null;
        RoleMapping.Submitter = null;
        RoleMapping.GroupReporter = null;
        RoleMapping.Reporter = null;
        RoleMapping.Reset();
    }

    [Fact]
    public void MappingRoles_SingleRole_MapsCorrectly()
    {
        RoleMapping.Submitter = "perm:heappe:exec";
        RoleMapping.Manager = "perm:heappe:manager";

        var mapping = RoleMapping.MappingRoles;

        mapping.Should().ContainKey("perm:heappe:exec");
        mapping["perm:heappe:exec"].Should().ContainSingle().Which.Should().Be("Submitter");

        mapping.Should().ContainKey("perm:heappe:manager");
        mapping["perm:heappe:manager"].Should().ContainSingle().Which.Should().Be("Manager");
    }

    [Fact]
    public void MappingRoles_MultipleRolesWithSamePermission_DoesNotThrow_AndIncludesAllRoles()
    {
        // This reproduces and verifies the fix for:
        // System.ArgumentException: An item with the same key has already been added. Key: perm:heappe:manager
        RoleMapping.Manager = "perm:heappe:manager";
        RoleMapping.Maintainer = "perm:heappe:manager";
        RoleMapping.ManagementAdmin = "perm:heappe:manager";

        var mapping = RoleMapping.MappingRoles;

        mapping.Should().ContainKey("perm:heappe:manager");
        mapping["perm:heappe:manager"].Should().BeEquivalentTo("Manager", "Maintainer", "ManagementAdmin");
    }

    [Fact]
    public void MappingRoles_CommaSeparatedPermissions_MapsAllPermissionsToRole()
    {
        RoleMapping.Submitter = "perm:heappe:exec, perm:heappe:run";

        var mapping = RoleMapping.MappingRoles;

        mapping.Should().ContainKey("perm:heappe:exec");
        mapping["perm:heappe:exec"].Should().ContainSingle().Which.Should().Be("Submitter");

        mapping.Should().ContainKey("perm:heappe:run");
        mapping["perm:heappe:run"].Should().ContainSingle().Which.Should().Be("Submitter");
    }

    [Fact]
    public void MappingRoles_CaseInsensitive_ResolvesRegardlessOfCase()
    {
        RoleMapping.Manager = "perm:heappe:manager";

        var mapping = RoleMapping.MappingRoles;

        mapping.ContainsKey("PERM:HEAPPE:MANAGER").Should().BeTrue();
        mapping["PERM:HEAPPE:MANAGER"].Should().Contain("Manager");

        RoleMapping.GetRolesForPermission("PERM:HEAPPE:MANAGER").Should().Contain("Manager");
    }

    [Fact]
    public void RoleMapping_Reset_OnPropertyChange_UpdatesMapping()
    {
        RoleMapping.Manager = "perm:heappe:manager";
        RoleMapping.MappingRoles.Should().ContainKey("perm:heappe:manager");

        // Updating property invalidates cached mapping
        RoleMapping.Manager = "perm:heappe:lead";

        var updatedMapping = RoleMapping.MappingRoles;
        updatedMapping.Should().NotContainKey("perm:heappe:manager");
        updatedMapping.Should().ContainKey("perm:heappe:lead");
        updatedMapping["perm:heappe:lead"].Should().Contain("Manager");
    }

    [Fact]
    public void GetRolesForPermission_UnmappedOrNull_ReturnsEmpty()
    {
        RoleMapping.GetRolesForPermission("nonexistent:permission").Should().BeEmpty();
        RoleMapping.GetRolesForPermission(null).Should().BeEmpty();
        RoleMapping.GetRolesForPermission("").Should().BeEmpty();
    }

    [Fact]
    public void MappingRoles_ConcurrentAccess_IsThreadSafe()
    {
        RoleMapping.Submitter = "perm:heappe:exec";
        RoleMapping.Manager = "perm:heappe:manager";
        RoleMapping.Maintainer = "perm:heappe:manager";

        // Concurrently access MappingRoles across multiple threads
        Parallel.For(0, 100, _ =>
        {
            var mapping = RoleMapping.MappingRoles;
            mapping.Should().NotBeNull();
            mapping.ContainsKey("perm:heappe:manager").Should().BeTrue();
            mapping["perm:heappe:manager"].Should().Contain("Manager");
            mapping["perm:heappe:manager"].Should().Contain("Maintainer");
        });
    }
}
