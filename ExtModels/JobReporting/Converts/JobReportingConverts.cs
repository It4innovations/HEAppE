﻿using System;
using System.Linq;
using HEAppE.DomainObjects.JobReporting;
using HEAppE.DomainObjects.JobReporting.Enums;
using HEAppE.ExtModels.JobManagement.Converts;
using HEAppE.ExtModels.JobReporting.Models;
using HEAppE.ExtModels.JobReporting.Models.DetailedReport;
using HEAppE.ExtModels.JobReporting.Models.ListReport;

namespace HEAppE.ExtModels.JobReporting.Converts;

public static class JobReportingConverts
{
    #region Public Methods

    public static UsageTypeExt ConvertIntToExt(this UsageType usageType)
    {
        _ = Enum.TryParse(usageType.ToString(), out UsageTypeExt convert);
        return convert;
    }

    public static TaskReportExt ConvertIntToExt(this TaskReport report)
    {
        var convert = new TaskReportExt
        {
            Id = report.SubmittedTaskInfo.Id,
            Name = report.SubmittedTaskInfo.Name,
            Usage = report.Usage
        };

        return convert;
    }

    public static TaskExtendedReportExt ConvertIntToExtendedExt(this TaskReport report)
    {
        var convert = new TaskExtendedReportExt
        {
            Id = report.SubmittedTaskInfo.Id,
            Name = report.SubmittedTaskInfo.Name,
            StartTime = report.SubmittedTaskInfo.StartTime,
            EndTime = report.SubmittedTaskInfo.EndTime,
            State = report.SubmittedTaskInfo.State.ConvertIntToExt(),
            CommandTemplateId = report.SubmittedTaskInfo.Specification.CommandTemplateId,
            Usage = report.Usage
        };

        return convert;
    }


    public static TaskDetailedReportExt ConvertIntToDetailedExt(this TaskReport report)
    {
        var convert = new TaskDetailedReportExt
        {
            Id = report.SubmittedTaskInfo.Id,
            ScheduledJobId = report.SubmittedTaskInfo.ScheduledJobId,
            Name = report.SubmittedTaskInfo.Name,
            StartTime = report.SubmittedTaskInfo.StartTime,
            EndTime = report.SubmittedTaskInfo.EndTime,
            State = report.SubmittedTaskInfo.State.ConvertIntToExt(),
            CommandTemplateId = report.SubmittedTaskInfo.Specification.CommandTemplateId,
            Usage = report.Usage,
            CommandTemplateName = report.SubmittedTaskInfo.Specification.CommandTemplate.Name
        };

        return convert;
    }

    public static JobReportExt ConvertIntToExt(this JobReport report)
    {
        var convert = new JobReportExt
        {
            Id = report.SubmittedJobInfo.Id,
            Name = report.SubmittedJobInfo.Name,
            SubProject = report.SubmittedJobInfo.Specification.SubProject?.Identifier,
            Tasks = report.Tasks.Select(x => x.ConvertIntToExt()).ToList()
        };

        return convert;
    }

    public static JobExtendedReportExt ConvertIntToExtendedExt(this JobReport report)
    {
        var convert = new JobExtendedReportExt
        {
            Id = report.SubmittedJobInfo.Id,
            Name = report.SubmittedJobInfo.Name,
            Tasks = report.Tasks.Select(x => x.ConvertIntToExtendedExt()).ToList(),
            State = report.SubmittedJobInfo.State.ConvertIntToExt()
        };

        return convert;
    }

    public static JobDetailedReportExt ConvertIntToDetailedExt(this JobReport report)
    {
        var convert = new JobDetailedReportExt
        {
            Id = report.SubmittedJobInfo.Id,
            Name = report.SubmittedJobInfo.Name,
            Tasks = report.Tasks.Select(x => x.ConvertIntToDetailedExt()).ToList(),
            State = report.SubmittedJobInfo.State.ConvertIntToExt(),
            CreationTime = report.SubmittedJobInfo.CreationTime,
            StartTime = report.SubmittedJobInfo.StartTime,
            SubmitTime = report.SubmittedJobInfo.SubmitTime,
            EndTime = report.SubmittedJobInfo.EndTime,
            Submitter = report.SubmittedJobInfo.Submitter.Username
        };

        return convert;
    }

    public static ClusterNodeTypeReportExt ConvertIntToExt(this ClusterNodeTypeReport report)
    {
        var convert = new ClusterNodeTypeReportExt
        {
            Id = report.ClusterNodeType.Id,
            Name = report.ClusterNodeType.Name,
            Jobs = report.Jobs.Select(x => x.ConvertIntToExt()).ToList(),
            TotalUsage = report.TotalUsage
        };

        return convert;
    }

    public static ClusterNodeExtendedTypeReportExt ConvertIntToExtendedExt(this ClusterNodeTypeReport report)
    {
        var convert = new ClusterNodeExtendedTypeReportExt
        {
            Id = report.ClusterNodeType.Id,
            Name = report.ClusterNodeType.Name,
            Description = report.ClusterNodeType.Description,
            Jobs = report.Jobs.Select(x => x.ConvertIntToExtendedExt()).ToList(),
            TotalUsage = report.TotalUsage
        };

        return convert;
    }

    public static ClusterNodeTypeDetailedReportExt ConvertIntToDetailedExt(this ClusterNodeTypeReport report)
    {
        var convert = new ClusterNodeTypeDetailedReportExt
        {
            Id = report.ClusterNodeType.Id,
            Name = report.ClusterNodeType.Name,
            Description = report.ClusterNodeType.Description,
            QueueName = report.ClusterNodeType.Queue,
            Jobs = report.Jobs.Select(x => x.ConvertIntToDetailedExt()).ToList(),
            TotalUsage = report.TotalUsage
        };

        return convert;
    }

    public static ClusterReportExt ConvertIntToExt(this ClusterReport report)
    {
        var convert = new ClusterReportExt
        {
            Id = report.Cluster.Id,
            Name = report.Cluster.Name,
            ClusterNodeTypes = report.ClusterNodeTypes.Select(x => x.ConvertIntToExt()).ToList(),
            TotalUsage = report.TotalUsage
        };

        return convert;
    }

    public static ClusterExtendedReportExt ConvertIntToExtendedExt(this ClusterReport report)
    {
        var convert = new ClusterExtendedReportExt
        {
            Id = report.Cluster.Id,
            Name = report.Cluster.Name,
            Description = report.Cluster.Description,
            ClusterNodeTypes = report.ClusterNodeTypes.Select(x => x.ConvertIntToExtendedExt()).ToList(),
            TotalUsage = report.TotalUsage
        };

        return convert;
    }


    public static ProjectReportExt ConvertIntToExt(this ProjectReport report)
    {
        var convert = new ProjectReportExt
        {
            Id = report.Project.Id,
            Name = report.Project.Name,
            AccountingString = report.Project.AccountingString,
            Clusters = report.Clusters.Select(x => x.ConvertIntToExt()).ToList(),
            TotalUsage = report.TotalUsage,
            UsageType = report.Project.UsageType.ConvertIntToExt()
        };

        return convert;
    }

    public static ProjectExtendedReportExt ConvertIntToExtendedExt(this ProjectReport report)
    {
        var convert = new ProjectExtendedReportExt
        {
            Id = report.Project.Id,
            Name = report.Project.Name,
            Description = report.Project.Description,
            AccountingString = report.Project.AccountingString,
            Clusters = report.Clusters.Select(x => x.ConvertIntToExtendedExt()).ToList(),
            TotalUsage = report.TotalUsage
        };

        return convert;
    }

    public static ProjectAggregatedReportExt ConvertIntToExt(this ProjectAggregatedReport report)
    {
        var convert = new ProjectAggregatedReportExt
        {
            Id = report.Project.Id,
            Name = report.Project.Name,
            AccountingString = report.Project.AccountingString,
            TotalUsage = report.TotalUsage,
            UsageType = report.Project.UsageType.ConvertIntToExt(),
            SubProjects = report.SubProjects.Select(x => x.ConvertIntToExt()).ToList(),
            Clusters = report.Clusters.Select(x => x.ConvertIntToExt()).ToList()
        };

        return convert;
    }

    public static SubProjectAggregatedReportExt ConvertIntToExt(this SubProjectAggregatedReport report)
    {
        var convert = new SubProjectAggregatedReportExt
        {
            Id = report.SubProject.Id,
            Name = report.SubProject.Identifier,
            TotalUsage = report.TotalUsage,
            Clusters = report.Clusters.Select(x => x.ConvertIntToExt()).ToList()
        };

        return convert;
    }

    public static ClusterAggregatedReportExt ConvertIntToExt(this ClusterAggregatedReport report)
    {
        var convert = new ClusterAggregatedReportExt
        {
            Id = report.Cluster.Id,
            Name = report.Cluster.Name,
            TotalUsage = report.TotalUsage,
            ClusterNodeTypesAggregations = report.ClusterNodeTypesAggregations.Select(x => x.ConvertIntToExt()).ToList()
        };

        return convert;
    }

    public static ClusterNodeTypeAggregatedReportExt ConvertIntToExt(this ClusterNodeTypeAggregatedReport report)
    {
        var convert = new ClusterNodeTypeAggregatedReportExt
        {
            Id = report.ClusterNodeTypeAggregation.Id,
            Name = report.ClusterNodeTypeAggregation.Name,
            TotalUsage = report.TotalUsage,
            ClusterNodeTypes = report.ClusterNodeTypes.Select(x => x.ConvertIntToExt()).ToList()
        };

        return convert;
    }

    public static ClusterDetailedReportExt ConvertIntToDetailedExt(this ClusterReport report)
    {
        var convert = new ClusterDetailedReportExt
        {
            Id = report.Cluster.Id,
            Name = report.Cluster.Name,
            Description = report.Cluster.Description,
            ClusterNodeTypes = report.ClusterNodeTypes.Select(x => x.ConvertIntToDetailedExt()).ToList(),
            TotalUsage = report.TotalUsage
        };
        return convert;
    }

    public static ProjectDetailedReportExt ConvertIntToDetailedExt(this ProjectReport report)
    {
        var convert = new ProjectDetailedReportExt
        {
            Id = report.Project.Id,
            Name = report.Project.Name,
            Description = report.Project.Description,
            AccountingString = report.Project.AccountingString,
            Clusters = report.Clusters.Select(x => x.ConvertIntToDetailedExt()).ToList(),
            TotalUsage = report.TotalUsage,
            UsageType = report.Project.UsageType.ConvertIntToExt(),
            StartDate = report.Project.StartDate,
            EndDate = report.Project.EndDate
        };

        return convert;
    }

    public static ProjectListReportExt ConvertIntToListExt(this ProjectReport report)
    {
        var convert = new ProjectListReportExt
        {
            Id = report.Project.Id,
            Name = report.Project.Name,
            Description = report.Project.Description,
            AccountingString = report.Project.AccountingString,
            TotalUsage = report.TotalUsage
        };

        return convert;
    }

    public static UserGroupListReportExt ConvertIntToExt(this UserGroupListReport report)
    {
        var convert = new UserGroupListReportExt
        {
            Id = report.AdaptorUserGroup.Id,
            Name = report.AdaptorUserGroup.Name,
            Description = report.AdaptorUserGroup.Description,
            Project = report.Project.ConvertIntToListExt(),
            TotalUsage = report.TotalUsage,
            UsageType = report.UsageType.ConvertIntToExt()
        };

        return convert;
    }

    public static UserGroupReportExt ConvertIntToExt(this UserGroupReport report)
    {
        var convert = new UserGroupReportExt
        {
            Id = report.AdaptorUserGroup.Id,
            Name = report.AdaptorUserGroup.Name,
            Description = report.AdaptorUserGroup.Description,
            Project = report.Project.ConvertIntToExt(),
            TotalUsage = report.TotalUsage,
            UsageType = report.UsageType.ConvertIntToExt()
        };

        return convert;
    }

    public static UserGroupDetailedReportExt ConvertIntToDetailedExt(this UserGroupReport report)
    {
        var convert = new UserGroupDetailedReportExt
        {
            Id = report.AdaptorUserGroup.Id,
            Name = report.AdaptorUserGroup.Name,
            Description = report.AdaptorUserGroup.Description,
            Project = report.Project.ConvertIntToDetailedExt(),
            TotalUsage = report.TotalUsage,
            UsageType = report.UsageType.ConvertIntToExt()
        };

        return convert;
    }


    public static UserResourceReportExt ConvertIntToExt(this UserResourceUsageReport report)
    {
        var convert = new UserResourceReportExt
        {
            UsageType = report.UsageType.ConvertIntToExt(),
            TotalUsage = report.TotalUsage,
            Projects = report.Projects.Select(x => x.ConvertIntToExt()).ToList()
        };

        return convert;
    }


    public static JobStateAggregationReportExt ConvertIntToExt(this JobStateAggregationReport aggregateJob)
    {
        var state = aggregateJob.State.ConvertIntToExt();
        var jobstateAggregationExt = new JobStateAggregationReportExt
        {
            JobStateId = state,
            JobStateName = state.ToString(),
            Count = aggregateJob.Count
        };

        return jobstateAggregationExt;
    }

    #endregion

    #region Private Methods

    #endregion
}