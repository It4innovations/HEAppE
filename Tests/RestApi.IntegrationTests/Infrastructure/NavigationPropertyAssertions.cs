using System.Collections.Generic;
using FluentAssertions;
using HEAppE.ExtModels.ClusterInformation.Models;
using HEAppE.ExtModels.JobManagement.Models;
using HEAppE.ExtModels.Management.Models;
using HEAppE.ExtModels.UserAndLimitationManagement.Models;

namespace HEAppE.RestApi.IntegrationTests.Infrastructure
{
    public static class NavigationPropertyAssertions
    {
        public static void ShouldHaveLoadedNodeTypes(this ClusterExt cluster)
        {
            cluster.NodeTypes.Should().NotBeNull("because ClusterExt.NodeTypes should be loaded via .Include()");
        }

        public static void ShouldHaveLoadedFileTransferMethods(this ClusterExt cluster)
        {
            cluster.FileTransferMethodIds.Should().NotBeNull("because ClusterExt.FileTransferMethodIds should be loaded via .Include()");
        }

        public static void ShouldHaveFullObjectGraph(this ClusterExt cluster)
        {
            cluster.ShouldHaveLoadedNodeTypes();
            cluster.ShouldHaveLoadedFileTransferMethods();

            if (cluster.NodeTypes != null)
            {
                foreach (var nodeType in cluster.NodeTypes)
                {
                    nodeType.ShouldHaveLoadedProjects();
                }
            }
        }

        public static void ShouldHaveLoadedNodeTypes(this ExtendedClusterExt cluster)
        {
            cluster.NodeTypes.Should().NotBeNull("because ExtendedClusterExt.NodeTypes should be loaded via .Include()");
        }

        public static void ShouldHaveLoadedFileTransferMethods(this ExtendedClusterExt cluster)
        {
            cluster.FileTransferMethodIds.Should().NotBeNull("because ExtendedClusterExt.FileTransferMethodIds should be loaded via .Include()");
        }

        public static void ShouldHaveFullObjectGraph(this ExtendedClusterExt cluster)
        {
            cluster.ShouldHaveLoadedNodeTypes();
            cluster.ShouldHaveLoadedFileTransferMethods();

            if (cluster.NodeTypes != null)
            {
                foreach (var nodeType in cluster.NodeTypes)
                {
                    nodeType.ShouldHaveLoadedProjects();
                }
            }
        }

        public static void ShouldHaveLoadedProjects(this ClusterNodeTypeExt nodeType)
        {
            nodeType.Projects.Should().NotBeNull("because ClusterNodeTypeExt.Projects should be loaded via .Include()");
            
            if (nodeType.Projects != null)
            {
                foreach (var project in nodeType.Projects)
                {
                    project.ShouldHaveLoadedCommandTemplates();
                }
            }
        }

        public static void ShouldHaveLoadedCommandTemplates(this ProjectExt project)
        {
            project.CommandTemplates.Should().NotBeNull("because ProjectExt.CommandTemplates should be loaded via .Include()");
            
            if (project.CommandTemplates != null)
            {
                foreach (var ct in project.CommandTemplates)
                {
                    ct.ShouldHaveLoadedTemplateParameters();
                }
            }
        }

        public static void ShouldHaveLoadedClusterProjectStoragePaths(this ProjectExt project)
        {
            project.ClusterProjectStoragePaths.Should().NotBeNull("because ProjectExt.ClusterProjectStoragePaths should be loaded via .Include()");
        }

        public static void ShouldHaveLoadedTemplateParameters(this CommandTemplateExt ct)
        {
            ct.TemplateParameters.Should().NotBeNull("because CommandTemplateExt.TemplateParameters should be loaded via .Include()");
        }

        public static void ShouldHaveLoadedTasks(this SubmittedJobInfoExt job)
        {
            job.Tasks.Should().NotBeNull("because SubmittedJobInfoExt.Tasks should be loaded via .Include()");
            job.Tasks.Should().NotBeEmpty("because SubmittedJobInfoExt.Tasks should not be empty for seeded data");
        }

        public static void ShouldHaveLoadedGroups(this AdaptorUserExt user)
        {
            user.AdaptorUserGroups.Should().NotBeNull("because AdaptorUserExt.AdaptorUserGroups should be loaded via .Include()");
        }
    }
}
