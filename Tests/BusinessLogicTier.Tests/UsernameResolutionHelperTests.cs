using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using FluentAssertions;
using HEAppE.BusinessLogicTier.AuthMiddleware;
using HEAppE.BusinessLogicTier.Configuration;
using HEAppE.BusinessLogicTier.Logic.ClusterInformation;
using HEAppE.DataAccessTier.IRepository.JobManagement;
using HEAppE.DataAccessTier.UnitOfWork;
using HEAppE.DomainObjects.ClusterInformation;
using HEAppE.DomainObjects.JobManagement;
using HEAppE.Services.Expirio;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Moq;
using SshCaAPI;
using Xunit;

namespace HEAppE.BusinessLogicTier.Tests;

[Trait("Category", "Unit")]
public class UsernameResolutionHelperTests
{
    private readonly Mock<IUnitOfWork> _mockUnitOfWork = new();
    private readonly Mock<IClusterProjectRepository> _mockClusterProjectRepo = new();
    private readonly Mock<ISshCertificateAuthorityService> _mockSshCaService = new();
    private readonly Mock<IHttpContextKeys> _mockHttpContextKeys = new();
    private readonly Mock<IExpirioService> _mockExpirioService = new();
    private readonly Mock<ILogger> _mockLogger = new();
    private readonly Mock<IRequestContext> _mockRequestContext = new();

    public UsernameResolutionHelperTests()
    {
        _mockHttpContextKeys.Setup(x => x.Context).Returns(_mockRequestContext.Object);
        _mockUnitOfWork.Setup(x => x.ClusterProjectRepository).Returns(_mockClusterProjectRepo.Object);

        var emptyClusterProjects = new List<ClusterProject>().AsQueryable();
        _mockClusterProjectRepo.Setup(x => x.AsQueryable()).Returns(emptyClusterProjects);
    }

    private static string CreateJwtToken(string preferredUsername)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim("preferred_username", preferredUsername)
            }),
            Expires = DateTime.UtcNow.AddHours(1)
        };
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    [Fact]
    public async Task ResolveUsernameFromContextAsync_WhenValidPosixUsername_ShouldReturnResolvedUsername()
    {
        var validToken = CreateJwtToken("posixuser");
        _mockRequestContext.Setup(x => x.IdpToken).Returns(validToken);

        var project = new Project
        {
            Id = 1,
            IsOneToOneMapping = true,
            AccountingString = "proj1"
        };

        var resolved = await UsernameResolutionHelper.ResolveUsernameFromContextAsync(
            _mockUnitOfWork.Object,
            _mockSshCaService.Object,
            _mockHttpContextKeys.Object,
            _mockExpirioService.Object,
            _mockLogger.Object,
            adaptorUserId: 4,
            project: project);

        resolved.Should().Be("posixuser");
    }

    [Fact]
    public async Task ResolveUsernameFromContextAsync_WhenPreferredUsernameContainsAtSign_ShouldIgnoreAndReturnNull()
    {
        var emailStyleToken = CreateJwtToken("485aea24-3d23-4e6d-b0b0-447f14626dd0@myaccessid.org");
        _mockRequestContext.Setup(x => x.IdpToken).Returns(emailStyleToken);

        var project = new Project
        {
            Id = 1,
            IsOneToOneMapping = true,
            AccountingString = "proj1"
        };

        var resolved = await UsernameResolutionHelper.ResolveUsernameFromContextAsync(
            _mockUnitOfWork.Object,
            _mockSshCaService.Object,
            _mockHttpContextKeys.Object,
            _mockExpirioService.Object,
            _mockLogger.Object,
            adaptorUserId: 4,
            project: project);

        resolved.Should().BeNull();
    }
}
