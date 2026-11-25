using System.Collections.Generic;
using FluentAssertions;
using HEAppE.BusinessLogicTier.AuthMiddleware;
using HEAppE.BusinessLogicTier.Logic.Management;
using HEAppE.DataAccessTier.IRepository.ClusterInformation;
using HEAppE.DataAccessTier.IRepository.JobManagement;
using HEAppE.DataAccessTier.UnitOfWork;
using HEAppE.DomainObjects.ClusterInformation;
using HEAppE.DomainObjects.JobManagement;
using HEAppE.ExtModels.Management.Converts;
using HEAppE.Services.Expirio;
using Microsoft.Extensions.Logging;
using Moq;
using SshCaAPI;
using Xunit;

namespace HEAppE.BusinessLogicTier.Tests;

[Trait("Category", "Unit")]
public class ProjectAssignmentPreferredAuthTypeTests
{
    private readonly Mock<IUnitOfWork> _mockUnitOfWork = new();
    private readonly Mock<IProjectRepository> _mockProjectRepo = new();
    private readonly Mock<IClusterRepository> _mockClusterRepo = new();
    private readonly Mock<IClusterProjectRepository> _mockClusterProjectRepo = new();
    private readonly Mock<ISshCertificateAuthorityService> _mockSshCa = new();
    private readonly Mock<IHttpContextKeys> _mockHttpContextKeys = new();
    private readonly Mock<IExpirioService> _mockExpirioService = new();
    private readonly Mock<ILogger> _mockLogger = new();

    public ProjectAssignmentPreferredAuthTypeTests()
    {
        _mockUnitOfWork.Setup(u => u.ProjectRepository).Returns(_mockProjectRepo.Object);
        _mockUnitOfWork.Setup(u => u.ClusterRepository).Returns(_mockClusterRepo.Object);
        _mockUnitOfWork.Setup(u => u.ClusterProjectRepository).Returns(_mockClusterProjectRepo.Object);
    }

    private ManagementLogic CreateLogic()
    {
        return new ManagementLogic(
            _mockUnitOfWork.Object,
            _mockSshCa.Object,
            _mockHttpContextKeys.Object,
            _mockExpirioService.Object,
            _mockLogger.Object);
    }

    [Theory]
    [InlineData(ClusterAuthenticationCredentialsAuthType.Unknown, ClusterAuthenticationCredentialsAuthTypeExt.Unknown)]
    [InlineData(ClusterAuthenticationCredentialsAuthType.Password, ClusterAuthenticationCredentialsAuthTypeExt.Password)]
    [InlineData(ClusterAuthenticationCredentialsAuthType.PrivateKey, ClusterAuthenticationCredentialsAuthTypeExt.PrivateKey)]
    [InlineData(ClusterAuthenticationCredentialsAuthType.PrivateKeyInVaultAndInSshAgent, ClusterAuthenticationCredentialsAuthTypeExt.PrivateKeyInVaultAndInSshAgent)]
    [InlineData(ClusterAuthenticationCredentialsAuthType.SshCertificate, ClusterAuthenticationCredentialsAuthTypeExt.SshCertificate)]
    [InlineData(ClusterAuthenticationCredentialsAuthType.Kerberos, ClusterAuthenticationCredentialsAuthTypeExt.Kerberos)]
    public void CreateProjectAssignmentToCluster_SetsAndConvertsPreferredAuthType(
        ClusterAuthenticationCredentialsAuthType inputAuthType,
        ClusterAuthenticationCredentialsAuthTypeExt expectedExt)
    {
        var project = new Project { Id = 1, ClusterProjects = new List<ClusterProject>() };
        var cluster = new Cluster { Id = 2 };

        _mockProjectRepo.Setup(r => r.GetById(1)).Returns(project);
        _mockClusterRepo.Setup(r => r.GetById(2)).Returns(cluster);
        _mockClusterProjectRepo
            .Setup(r => r.GetClusterProjectForClusterAndProjectIncludingDeleted(2, 1))
            .Returns((ClusterProject)null!);
        _mockClusterProjectRepo
            .Setup(r => r.GetClusterProjectForProject(1))
            .Returns(new List<ClusterProject>());

        ClusterProject? inserted = null;
        _mockClusterProjectRepo
            .Setup(r => r.Insert(It.IsAny<ClusterProject>()))
            .Callback<ClusterProject>(cp => inserted = cp);

        var logic = CreateLogic();
        var result = logic.CreateProjectAssignmentToCluster(1, 2, "/scratch", "/project", inputAuthType);

        inserted.Should().NotBeNull();
        inserted!.PreferredAuthType.Should().Be(inputAuthType);
        result.PreferredAuthType.Should().Be(inputAuthType);
        result.ConvertIntToExt().PreferredAuthType.Should().Be(expectedExt);
    }

    [Theory]
    [InlineData(ClusterAuthenticationCredentialsAuthType.Unknown, ClusterAuthenticationCredentialsAuthTypeExt.Unknown)]
    [InlineData(ClusterAuthenticationCredentialsAuthType.Password, ClusterAuthenticationCredentialsAuthTypeExt.Password)]
    [InlineData(ClusterAuthenticationCredentialsAuthType.PrivateKeyInVaultAndInSshAgent, ClusterAuthenticationCredentialsAuthTypeExt.PrivateKeyInVaultAndInSshAgent)]
    [InlineData(ClusterAuthenticationCredentialsAuthType.Kerberos, ClusterAuthenticationCredentialsAuthTypeExt.Kerberos)]
    public void CreateProjectAssignmentToCluster_ReactivatingDeletedAssignment_UpdatesPreferredAuthType(
        ClusterAuthenticationCredentialsAuthType inputAuthType,
        ClusterAuthenticationCredentialsAuthTypeExt expectedExt)
    {
        var project = new Project { Id = 1 };
        var cluster = new Cluster { Id = 2 };
        var deletedAssignment = new ClusterProject
        {
            Id = 5,
            ProjectId = 1,
            ClusterId = 2,
            IsDeleted = true,
            PreferredAuthType = ClusterAuthenticationCredentialsAuthType.PrivateKey,
            ClusterProjectCredentials = new List<ClusterProjectCredential>()
        };

        _mockProjectRepo.Setup(r => r.GetById(1)).Returns(project);
        _mockClusterRepo.Setup(r => r.GetById(2)).Returns(cluster);
        _mockClusterProjectRepo
            .Setup(r => r.GetClusterProjectForClusterAndProjectIncludingDeleted(2, 1))
            .Returns(deletedAssignment);

        var logic = CreateLogic();
        var result = logic.CreateProjectAssignmentToCluster(1, 2, "/scratch", "/project", inputAuthType);

        result.IsDeleted.Should().BeFalse();
        result.PreferredAuthType.Should().Be(inputAuthType);
        result.ConvertIntToExt().PreferredAuthType.Should().Be(expectedExt);
    }

    [Theory]
    [InlineData(ClusterAuthenticationCredentialsAuthType.Unknown, ClusterAuthenticationCredentialsAuthTypeExt.Unknown)]
    [InlineData(ClusterAuthenticationCredentialsAuthType.Password, ClusterAuthenticationCredentialsAuthTypeExt.Password)]
    [InlineData(ClusterAuthenticationCredentialsAuthType.PrivateKeyInVaultAndInSshAgent, ClusterAuthenticationCredentialsAuthTypeExt.PrivateKeyInVaultAndInSshAgent)]
    [InlineData(ClusterAuthenticationCredentialsAuthType.Kerberos, ClusterAuthenticationCredentialsAuthTypeExt.Kerberos)]
    public void ModifyProjectAssignmentToCluster_UpdatesAndConvertsPreferredAuthType(
        ClusterAuthenticationCredentialsAuthType inputAuthType,
        ClusterAuthenticationCredentialsAuthTypeExt expectedExt)
    {
        var project = new Project { Id = 1 };
        var cluster = new Cluster { Id = 2 };
        var existingAssignment = new ClusterProject
        {
            Id = 5,
            ProjectId = 1,
            ClusterId = 2,
            IsDeleted = false,
            Project = project,
            Cluster = cluster,
            PreferredAuthType = ClusterAuthenticationCredentialsAuthType.PrivateKey
        };

        _mockClusterProjectRepo
            .Setup(r => r.GetClusterProjectForClusterAndProject(2, 1))
            .Returns(existingAssignment);
        _mockClusterRepo.Setup(r => r.GetById(2)).Returns(cluster);

        var logic = CreateLogic();
        var result = logic.ModifyProjectAssignmentToCluster(1, 2, "/scratch2", "/project2", inputAuthType);

        result.PreferredAuthType.Should().Be(inputAuthType);
        result.ConvertIntToExt().PreferredAuthType.Should().Be(expectedExt);
    }
}
