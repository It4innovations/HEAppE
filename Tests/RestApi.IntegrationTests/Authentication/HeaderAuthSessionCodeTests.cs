using System;
using FluentAssertions;
using HEAppE.BusinessLogicTier.AuthMiddleware;
using HEAppE.BusinessLogicTier.Logic.UserAndLimitationManagement;
using HEAppE.DomainObjects.UserAndLimitationManagement;
using HEAppE.ServiceTier.UserAndLimitationManagement;
using Moq;
using Xunit;

namespace HEAppE.RestApi.IntegrationTests.Authentication;

[Trait("Category", "Unit")]
public class HeaderAuthSessionCodeTests
{
    [Theory]
    [InlineData("HEADER-AUTH-NEEDED")]
    [InlineData("header-auth-needed")]
    [InlineData("")]
    [InlineData(null)]
    public void AuthenticateUser_WithSentinelOrEmptySessionCode_ResolvesFromHttpContextKeys(string? sessionCode)
    {
        // Arrange
        var mockContext = new Mock<IRequestContext>();
        mockContext.Setup(c => c.AdaptorUserId).Returns(42);

        var mockHttpContextKeys = new Mock<IHttpContextKeys>();
        mockHttpContextKeys.Setup(h => h.Context).Returns(mockContext.Object);

        var expectedUser = new AdaptorUser
        {
            Id = 42,
            Username = "user1",
            Email = "user1@example.com"
        };

        var mockAuthLogic = new Mock<IUserAndLimitationManagementLogic>();
        mockAuthLogic.Setup(l => l.GetUserById(42)).Returns(expectedUser);

        // Act
        var result = UserAndLimitationManagementService.AuthenticateUser(sessionCode, mockAuthLogic.Object, mockHttpContextKeys.Object);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(42);
        result.Username.Should().Be("user1");
        mockAuthLogic.Verify(l => l.GetUserById(42), Times.Once);
        mockAuthLogic.Verify(l => l.GetUserForSessionCode(It.IsAny<string>()), Times.Never);
    }

    [Theory]
    [InlineData("HEADER-AUTH-NEEDED")]
    [InlineData("header-auth-needed")]
    [InlineData("")]
    [InlineData(null)]
    public void AuthenticateUser_WithSentinelOrEmptySessionCode_ThrowsUnauthorized_WhenAdaptorUserIdInvalid(string? sessionCode)
    {
        // Arrange
        var mockContext = new Mock<IRequestContext>();
        mockContext.Setup(c => c.AdaptorUserId).Returns(0);

        var mockHttpContextKeys = new Mock<IHttpContextKeys>();
        mockHttpContextKeys.Setup(h => h.Context).Returns(mockContext.Object);

        var mockAuthLogic = new Mock<IUserAndLimitationManagementLogic>();

        // Act
        var act = () => UserAndLimitationManagementService.AuthenticateUser(sessionCode, mockAuthLogic.Object, mockHttpContextKeys.Object);

        // Assert
        act.Should().Throw<UnauthorizedAccessException>();
        mockAuthLogic.Verify(l => l.GetUserForSessionCode(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public void AuthenticateUser_WithValidSessionCode_CallsGetUserForSessionCode()
    {
        // Arrange
        const string validSessionCode = "abcdef12-3456-7890-abcd-ef1234567890";
        var mockContext = new Mock<IRequestContext>();
        var mockHttpContextKeys = new Mock<IHttpContextKeys>();
        mockHttpContextKeys.Setup(h => h.Context).Returns(mockContext.Object);

        var expectedUser = new AdaptorUser
        {
            Id = 10,
            Username = "user2",
            Email = "user2@example.com"
        };

        var mockAuthLogic = new Mock<IUserAndLimitationManagementLogic>();
        mockAuthLogic.Setup(l => l.GetUserForSessionCode(validSessionCode)).Returns(expectedUser);

        // Act
        var result = UserAndLimitationManagementService.AuthenticateUser(validSessionCode, mockAuthLogic.Object, mockHttpContextKeys.Object);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(10);
        result.Username.Should().Be("user2");
        mockAuthLogic.Verify(l => l.GetUserForSessionCode(validSessionCode), Times.Once);
        mockAuthLogic.Verify(l => l.GetUserById(It.IsAny<long>()), Times.Never);
    }
}
