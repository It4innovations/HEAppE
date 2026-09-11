using System;
using System.Linq;
using HEAppE.DataAccessTier;
using HEAppE.DomainObjects.UserAndLimitationManagement;
using Microsoft.EntityFrameworkCore;
using Xunit;
using FluentAssertions;

namespace HEAppE.DataAccessTier.Tests;

[Trait("Category", "Unit")]
public class EntityCrudTests
{
    [Fact]
    public void AdaptorUser_Crud_InMemoryDatabase()
    {
        var options = new DbContextOptionsBuilder<MiddlewareContext>()
            .UseInMemoryDatabase(databaseName: "Test_User_Crud_Db")
            .Options;

        using (var context = new MiddlewareContext(options))
        {
            var user = new AdaptorUser
            {
                Id = 1,
                Username = "testuser",
                Password = "hashedpassword",
                CreatedAt = DateTime.UtcNow
            };
            context.AdaptorUsers.Add(user);
            context.SaveChanges();
        }

        using (var context = new MiddlewareContext(options))
        {
            var fetched = context.AdaptorUsers.FirstOrDefault(u => u.Username == "testuser");
            fetched.Should().NotBeNull();
            fetched.Username.Should().Be("testuser");
        }
    }
}
