using System;
using System.Linq;
using HEAppE.DataAccessTier;
using Microsoft.EntityFrameworkCore;
using Xunit;
using FluentAssertions;

namespace HEAppE.DataAccessTier.Tests;

[Trait("Category", "Unit")]
public class MigrationTests
{
    [Fact]
    public void MiddlewareContext_ModelCreation_ValidatesEntityDefinitions()
    {
        var options = new DbContextOptionsBuilder<MiddlewareContext>()
            .UseInMemoryDatabase(databaseName: "Test_Model_Validation_Db")
            .Options;

        using var context = new MiddlewareContext(options);
        
        // Assert that core DbSets are configured
        context.AdaptorUsers.Should().NotBeNull();
        context.Clusters.Should().NotBeNull();
        context.Projects.Should().NotBeNull();
        context.ClusterNodeTypes.Should().NotBeNull();
        context.CommandTemplates.Should().NotBeNull();
    }
}
