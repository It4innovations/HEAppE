using System;
using System.Collections.Generic;
using FluentAssertions;
using HEAppE.BusinessLogicTier.AuthMiddleware;
using HEAppE.BusinessLogicTier.Logic.Management;
using HEAppE.DataAccessTier.IRepository.JobManagement;
using HEAppE.DataAccessTier.UnitOfWork;
using HEAppE.DomainObjects.JobManagement;
using HEAppE.DomainObjects.JobReporting.Enums;
using HEAppE.Exceptions.External;
using HEAppE.Services.Expirio;
using Microsoft.Extensions.Logging;
using Moq;
using SshCaAPI;
using Xunit;

namespace HEAppE.BusinessLogicTier.Tests;

[Trait("Category", "Unit")]
public class ManagementLogicSubProjectTests
{
    private readonly Mock<IUnitOfWork> _mockUnitOfWork = new();
    private readonly Mock<IProjectRepository> _mockProjectRepo = new();
    private readonly Mock<ISubProjectRepository> _mockSubProjectRepo = new();
    private readonly Mock<ISshCertificateAuthorityService> _mockSshCa = new();
    private readonly Mock<IHttpContextKeys> _mockHttpContextKeys = new();
    private readonly Mock<IExpirioService> _mockExpirioService = new();
    private readonly Mock<ILogger> _mockLogger = new();

    public ManagementLogicSubProjectTests()
    {
        _mockUnitOfWork.Setup(u => u.ProjectRepository).Returns(_mockProjectRepo.Object);
        _mockUnitOfWork.Setup(u => u.SubProjectRepository).Returns(_mockSubProjectRepo.Object);
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

    [Fact]
    public void ModifyProject_WhenProjectEndDateExtended_ExtendsMatchingAndExpiredSubProjects()
    {
        // Arrange
        var projectId = 1L;
        var oldStartDate = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var oldEndDate = new DateTime(2025, 12, 31, 23, 59, 59, DateTimeKind.Utc);
        var newEndDate = new DateTime(2026, 12, 31, 23, 59, 59, DateTimeKind.Utc);

        var project = new Project
        {
            Id = projectId,
            Name = "TestProject",
            StartDate = oldStartDate,
            EndDate = oldEndDate,
            IsDeleted = false
        };

        var subProject1 = new SubProject
        {
            Id = 10,
            Identifier = "sub-1",
            ProjectId = projectId,
            StartDate = oldStartDate,
            EndDate = oldEndDate,
            IsDeleted = false
        };

        var subProject2 = new SubProject
        {
            Id = 20,
            Identifier = "sub-2",
            ProjectId = projectId,
            StartDate = oldStartDate,
            EndDate = DateTime.UtcNow.AddDays(-10), // expired
            IsDeleted = false
        };

        _mockProjectRepo.Setup(r => r.GetById(projectId)).Returns(project);
        _mockSubProjectRepo.Setup(r => r.GetSubProjectsForProject(projectId)).Returns(new List<SubProject> { subProject1, subProject2 });

        var logic = CreateLogic();

        // Act
        var result = logic.ModifyProject(projectId, UsageType.NodeHours, "UpdatedName", "Desc", oldStartDate, newEndDate, true, false);

        // Assert
        result.EndDate.Should().Be(newEndDate);
        subProject1.EndDate.Should().Be(newEndDate);
        subProject2.EndDate.Should().Be(newEndDate);
        _mockSubProjectRepo.Verify(r => r.Update(subProject1), Times.Once);
        _mockSubProjectRepo.Verify(r => r.Update(subProject2), Times.Once);
        _mockUnitOfWork.Verify(u => u.Save(), Times.Once);
    }

    [Fact]
    public void ModifyProject_WhenProjectEndDateShortened_ClampsSubProjectsExceedingEndDate()
    {
        // Arrange
        var projectId = 1L;
        var oldStartDate = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var oldEndDate = new DateTime(2027, 12, 31, 23, 59, 59, DateTimeKind.Utc);
        var shortenedEndDate = new DateTime(2026, 6, 30, 23, 59, 59, DateTimeKind.Utc);

        var project = new Project
        {
            Id = projectId,
            Name = "TestProject",
            StartDate = oldStartDate,
            EndDate = oldEndDate,
            IsDeleted = false
        };

        var subProject = new SubProject
        {
            Id = 10,
            Identifier = "sub-1",
            ProjectId = projectId,
            StartDate = oldStartDate,
            EndDate = oldEndDate,
            IsDeleted = false
        };

        _mockProjectRepo.Setup(r => r.GetById(projectId)).Returns(project);
        _mockSubProjectRepo.Setup(r => r.GetSubProjectsForProject(projectId)).Returns(new List<SubProject> { subProject });

        var logic = CreateLogic();

        // Act
        logic.ModifyProject(projectId, UsageType.NodeHours, null!, null!, oldStartDate, shortenedEndDate, null, false);

        // Assert
        subProject.EndDate.Should().Be(shortenedEndDate);
        _mockSubProjectRepo.Verify(r => r.Update(subProject), Times.Once);
    }

    [Fact]
    public void CreateSubProject_WhenSubProjectExpiredAndProjectActive_AutoExtendsSubProject()
    {
        // Arrange
        var projectId = 1L;
        var activeProject = new Project
        {
            Id = projectId,
            StartDate = DateTime.UtcNow.AddMonths(-6),
            EndDate = DateTime.UtcNow.AddMonths(6),
            IsDeleted = false
        };

        var expiredSubProject = new SubProject
        {
            Id = 10,
            Identifier = "ehpc-aif-f",
            ProjectId = projectId,
            StartDate = DateTime.UtcNow.AddMonths(-6),
            EndDate = DateTime.UtcNow.AddDays(-5), // expired
            IsDeleted = false
        };

        _mockProjectRepo.Setup(r => r.GetById(projectId)).Returns(activeProject);
        _mockSubProjectRepo.Setup(r => r.GetByIdentifier("ehpc-aif-f", projectId)).Returns(expiredSubProject);

        var logic = CreateLogic();

        // Act
        var result = logic.CreateSubProject("ehpc-aif-f", projectId);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(10);
        result.EndDate.Should().Be(activeProject.EndDate);
        _mockSubProjectRepo.Verify(r => r.Update(expiredSubProject), Times.Once);
    }

    [Fact]
    public void CreateSubProject_WhenSubProjectDeleted_ThrowsInputValidationException()
    {
        // Arrange
        var projectId = 1L;
        var activeProject = new Project
        {
            Id = projectId,
            StartDate = DateTime.UtcNow.AddMonths(-6),
            EndDate = DateTime.UtcNow.AddMonths(6),
            IsDeleted = false
        };

        var deletedSubProject = new SubProject
        {
            Id = 10,
            Identifier = "ehpc-aif-f",
            ProjectId = projectId,
            StartDate = DateTime.UtcNow.AddMonths(-6),
            EndDate = DateTime.UtcNow.AddMonths(6),
            IsDeleted = true
        };

        _mockProjectRepo.Setup(r => r.GetById(projectId)).Returns(activeProject);
        _mockSubProjectRepo.Setup(r => r.GetByIdentifier("ehpc-aif-f", projectId)).Returns(deletedSubProject);

        var logic = CreateLogic();

        // Act & Assert
        var act = () => logic.CreateSubProject("ehpc-aif-f", projectId);
        act.Should().Throw<InputValidationException>()
            .WithMessage("*SubProjectDeletedOrEnded*");
    }

    [Fact]
    public void RemoveProject_SoftDeletesAssociatedSubProjects()
    {
        // Arrange
        var projectId = 1L;
        var project = new Project
        {
            Id = projectId,
            IsDeleted = false,
            ClusterProjects = new List<ClusterProject>(),
            ProjectClusterNodeTypeAggregations = new List<ProjectClusterNodeTypeAggregation>()
        };

        var subProject = new SubProject
        {
            Id = 10,
            Identifier = "sub-1",
            ProjectId = projectId,
            IsDeleted = false
        };

        _mockProjectRepo.Setup(r => r.GetById(projectId)).Returns(project);
        _mockSubProjectRepo.Setup(r => r.GetSubProjectsForProject(projectId)).Returns(new List<SubProject> { subProject });

        var logic = CreateLogic();

        // Act
        logic.RemoveProject(projectId);

        // Assert
        project.IsDeleted.Should().BeTrue();
        subProject.IsDeleted.Should().BeTrue();
        _mockSubProjectRepo.Verify(r => r.Update(subProject), Times.Once);
        _mockProjectRepo.Verify(r => r.Update(project), Times.Once);
        _mockUnitOfWork.Verify(u => u.Save(), Times.Once);
    }
}
