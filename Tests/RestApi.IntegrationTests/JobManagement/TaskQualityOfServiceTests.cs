using System.Collections.Generic;
using HEAppE.DomainObjects.ClusterInformation;
using HEAppE.DomainObjects.JobManagement;
using HEAppE.DomainObjects.JobManagement.JobInformation;
using HEAppE.DomainObjects.UserAndLimitationManagement;
using HEAppE.ExtModels.JobManagement.Converts;
using HEAppE.ExtModels.JobManagement.Models;
using HEAppE.HpcConnectionFramework.SchedulerAdapters.ConversionAdapter;
using HEAppE.HpcConnectionFramework.SchedulerAdapters.Slurm.Generic;
using HEAppE.RestApi.InputValidator;
using HEAppE.RestApiModels.JobManagement;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;
using FluentAssertions;

namespace HEAppE.RestApi.IntegrationTests.JobManagement;

[Trait("Category", "Unit")]
public class TaskQualityOfServiceTests
{
    [Fact]
    public void ConvertExtToInt_CopiesQualityOfService()
    {
        var jobSpecExt = new JobSpecificationExt
        {
            Name = "Job1",
            ProjectId = 1,
            Tasks = new[]
            {
                new TaskSpecificationExt
                {
                    Name = "Task1",
                    MinCores = 1,
                    MaxCores = 4,
                    QualityOfService = "debug",
                    ClusterNodeTypeId = 1
                }
            }
        };

        var intJobSpec = jobSpecExt.ConvertExtToInt(1, null);

        intJobSpec.Tasks.Should().HaveCount(1);
        intJobSpec.Tasks[0].QualityOfService.Should().Be("debug");
    }

    [Fact]
    public void ConvertToAdminSubmittedJobInfoExt_WithTaskQos_UsesTaskQualityOfService()
    {
        var jobInfo = new SubmittedJobInfo
        {
            Id = 1,
            Name = "Job1",
            Specification = new JobSpecification(),
            Tasks = new List<SubmittedTaskInfo>
            {
                new SubmittedTaskInfo
                {
                    Id = 1,
                    Name = "Task1",
                    Specification = new TaskSpecification
                    {
                        QualityOfService = "high"
                    },
                    NodeType = new ClusterNodeType
                    {
                        QualityOfService = "normal"
                    }
                }
            }
        };

        var adminJob = jobInfo.ConvertToAdminSubmittedJobInfoExt();

        adminJob.Tasks.Should().HaveCount(1);
        adminJob.Tasks[0].QualityOfService.Should().Be("high");
    }

    [Fact]
    public void ConvertToAdminSubmittedJobInfoExt_WithoutTaskQos_FallsBackToNodeTypeQualityOfService()
    {
        var jobInfo = new SubmittedJobInfo
        {
            Id = 1,
            Name = "Job1",
            Specification = new JobSpecification(),
            Tasks = new List<SubmittedTaskInfo>
            {
                new SubmittedTaskInfo
                {
                    Id = 1,
                    Name = "Task1",
                    Specification = new TaskSpecification
                    {
                        QualityOfService = null
                    },
                    NodeType = new ClusterNodeType
                    {
                        QualityOfService = "normal"
                    }
                }
            }
        };

        var adminJob = jobInfo.ConvertToAdminSubmittedJobInfoExt();

        adminJob.Tasks.Should().HaveCount(1);
        adminJob.Tasks[0].QualityOfService.Should().Be("normal");
    }

    [Fact]
    public void ConvertIntToExt_WithTaskQos_UsesTaskQualityOfService()
    {
        var jobInfo = new SubmittedJobInfo
        {
            Id = 1,
            Name = "Job1",
            Specification = new JobSpecification(),
            Tasks = new List<SubmittedTaskInfo>
            {
                new SubmittedTaskInfo
                {
                    Id = 1,
                    Name = "Task1",
                    Specification = new TaskSpecification
                    {
                        QualityOfService = "interactive"
                    },
                    NodeType = new ClusterNodeType
                    {
                        QualityOfService = "standard"
                    }
                }
            }
        };

        var extJob = jobInfo.ConvertIntToExt();

        extJob.Tasks.Should().HaveCount(1);
        extJob.Tasks[0].QualityOfService.Should().Be("interactive");
    }

    [Fact]
    public void ConvertIntToExt_WithoutTaskQos_FallsBackToNodeTypeQualityOfService()
    {
        var jobInfo = new SubmittedJobInfo
        {
            Id = 1,
            Name = "Job1",
            Specification = new JobSpecification(),
            Tasks = new List<SubmittedTaskInfo>
            {
                new SubmittedTaskInfo
                {
                    Id = 1,
                    Name = "Task1",
                    Specification = new TaskSpecification
                    {
                        QualityOfService = null
                    },
                    NodeType = new ClusterNodeType
                    {
                        QualityOfService = "standard"
                    }
                }
            }
        };

        var extJob = jobInfo.ConvertIntToExt();

        extJob.Tasks.Should().HaveCount(1);
        extJob.Tasks[0].QualityOfService.Should().Be("standard");
    }

    [Fact]
    public void JobManagementValidator_ValidQualityOfService_PassesValidation()
    {
        var jobSpec = new JobSpecificationExt
        {
            Name = "TestJob",
            ProjectId = 1,
            ClusterId = 1,
            Tasks = new[]
            {
                new TaskSpecificationExt
                {
                    Name = "Task1",
                    MinCores = 1,
                    MaxCores = 2,
                    ClusterNodeTypeId = 1,
                    StandardOutputFile = "stdout.txt",
                    StandardErrorFile = "stderr.txt",
                    ProgressFile = "prog.txt",
                    LogFile = "log.txt",
                    QualityOfService = "high"
                }
            }
        };

        var model = new CreateJobByProjectModel
        {
            SessionCode = "test-session",
            JobSpecification = jobSpec
        };

        var validator = new JobManagementValidator(model);
        var result = validator.Validate();

        result.IsValid.Should().BeTrue(result.Message);
    }

    [Fact]
    public void JobManagementValidator_TooLongQualityOfService_FailsValidation()
    {
        var jobSpec = new JobSpecificationExt
        {
            Name = "TestJob",
            ProjectId = 1,
            ClusterId = 1,
            Tasks = new[]
            {
                new TaskSpecificationExt
                {
                    Name = "Task1",
                    MinCores = 1,
                    MaxCores = 2,
                    ClusterNodeTypeId = 1,
                    StandardOutputFile = "stdout.txt",
                    StandardErrorFile = "stderr.txt",
                    ProgressFile = "prog.txt",
                    LogFile = "log.txt",
                    QualityOfService = new string('x', 1001)
                }
            }
        };

        var model = new CreateJobByProjectModel
        {
            SessionCode = "test-session",
            JobSpecification = jobSpec
        };

        var validator = new JobManagementValidator(model);
        var result = validator.Validate();

        result.IsValid.Should().BeFalse();
        result.Message.Should().Contain("Quality of service specification");
    }

    [Fact]
    public void SchedulerDataConvertor_TaskQualityOfServiceOverridesNodeTypeQualityOfService()
    {
        var mockFactory = new Mock<ConversionAdapterFactory>();
        var mockAdapter = new Mock<ISchedulerTaskAdapter>();
        mockFactory.Setup(f => f.CreateTaskAdapter(It.IsAny<object>())).Returns(mockAdapter.Object);

        var convertor = new SlurmDataConvertor(mockFactory.Object, NullLogger.Instance);

        var jobSpec = new JobSpecification
        {
            Id = 1,
            ProjectId = 1,
            ClusterUser = new ClusterAuthenticationCredentials { Username = "testuser" },
            Cluster = new Cluster
            {
                ClusterProjects = new List<ClusterProject>
                {
                    new ClusterProject { ProjectId = 1, ScratchStoragePath = "/scratch" }
                }
            }
        };

        var taskSpec = new TaskSpecification
        {
            Id = 123,
            Name = "TestTask",
            QualityOfService = "task-qos",
            ClusterNodeType = new ClusterNodeType
            {
                QualityOfService = "node-qos",
                Queue = "main"
            },
            Project = new Project(),
            CommandTemplate = new CommandTemplate { Name = "testTemplate", ExecutableFile = "test.sh" },
            JobSpecification = jobSpec
        };

        convertor.ConvertTaskSpecificationToTask(jobSpec, taskSpec, "");

        mockAdapter.VerifySet(a => a.QualityOfService = "task-qos", Times.Once());
    }

    [Fact]
    public void SchedulerDataConvertor_FallbackToNodeTypeQualityOfService_WhenTaskQosIsEmpty()
    {
        var mockFactory = new Mock<ConversionAdapterFactory>();
        var mockAdapter = new Mock<ISchedulerTaskAdapter>();
        mockFactory.Setup(f => f.CreateTaskAdapter(It.IsAny<object>())).Returns(mockAdapter.Object);

        var convertor = new SlurmDataConvertor(mockFactory.Object, NullLogger.Instance);

        var jobSpec = new JobSpecification
        {
            Id = 1,
            ProjectId = 1,
            ClusterUser = new ClusterAuthenticationCredentials { Username = "testuser" },
            Cluster = new Cluster
            {
                ClusterProjects = new List<ClusterProject>
                {
                    new ClusterProject { ProjectId = 1, ScratchStoragePath = "/scratch" }
                }
            }
        };

        var taskSpec = new TaskSpecification
        {
            Id = 123,
            Name = "TestTask",
            QualityOfService = null,
            ClusterNodeType = new ClusterNodeType
            {
                QualityOfService = "node-qos",
                Queue = "main"
            },
            Project = new Project(),
            CommandTemplate = new CommandTemplate { Name = "testTemplate", ExecutableFile = "test.sh" },
            JobSpecification = jobSpec
        };

        convertor.ConvertTaskSpecificationToTask(jobSpec, taskSpec, "");

        mockAdapter.VerifySet(a => a.QualityOfService = "node-qos", Times.Once());
    }
}
