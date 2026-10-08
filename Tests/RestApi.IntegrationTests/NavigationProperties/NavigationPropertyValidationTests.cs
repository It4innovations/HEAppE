using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using HEAppE.ExtModels.ClusterInformation.Models;
using HEAppE.ExtModels.JobManagement.Models;
using HEAppE.ExtModels.Management.Models;
using HEAppE.ExtModels.UserAndLimitationManagement.Models;
using HEAppE.RestApi.IntegrationTests.Infrastructure;
using HEAppE.RestApi.IntegrationTests.Management;
using Xunit;

namespace HEAppE.RestApi.IntegrationTests.NavigationProperties
{
    [Trait("Category", "Integration")]
    public class NavigationPropertyValidationTests : ManagementTestBase
    {
        public NavigationPropertyValidationTests(HEAppEWebApplicationFactory factory) : base(factory)
        {
        }

        #region Management Clusters

        [Fact]
        public async Task ManagementClusters_ShouldLoadNavigationProperties()
        {
            var sessionCode = await GetAdminSessionCodeAsync();
            var url = $"/heappe/Management/Clusters?sessionCode={sessionCode}";
            
            var clusters = await _client.GetJsonAsync<List<ExtendedClusterExt>>(url);
            
            clusters.Should().NotBeNull();
            clusters.Should().NotBeEmpty();
            
            foreach (var cluster in clusters)
            {
                cluster.NodeTypes.Should().NotBeNull();
                cluster.FileTransferMethodIds.Should().NotBeNull();
            }
        }

        [Fact]
        public async Task ManagementCluster_ShouldLoadNavigationProperties()
        {
            var sessionCode = await GetAdminSessionCodeAsync();
            
            // First get all to pick an ID
            var clustersUrl = $"/heappe/Management/Clusters?sessionCode={sessionCode}";
            var clusters = await _client.GetJsonAsync<List<ExtendedClusterExt>>(clustersUrl);
            clusters.Should().NotBeNullOrEmpty();
            var clusterId = clusters.First().Id;

            var url = $"/heappe/Management/Cluster?id={clusterId}&sessionCode={sessionCode}";
            var cluster = await _client.GetJsonAsync<ExtendedClusterExt>(url);
            
            cluster.Should().NotBeNull();
            cluster.NodeTypes.Should().NotBeNull();
            cluster.SchedulerType.Should().BeDefined();
        }

        #endregion

        #region Management Projects

        [Fact]
        public async Task ManagementProjects_ShouldLoadNavigationProperties()
        {
            var sessionCode = await GetAdminSessionCodeAsync();
            var url = $"/heappe/Management/Projects?sessionCode={sessionCode}";
            
            var projects = await _client.GetJsonAsync<List<ProjectExt>>(url);
            
            projects.Should().NotBeNull();
            projects.Should().NotBeEmpty();
            
            foreach (var project in projects)
            {
                // Bug A: Missing Include for CommandTemplates
                project.CommandTemplates.Should().NotBeNull();
            }
        }

        [Fact]
        public async Task ManagementProject_ShouldLoadNavigationProperties()
        {
            var sessionCode = await GetAdminSessionCodeAsync();
            
            var projectsUrl = $"/heappe/Management/Projects?sessionCode={sessionCode}";
            var projects = await _client.GetJsonAsync<List<ProjectExt>>(projectsUrl);
            projects.Should().NotBeNullOrEmpty();
            var projectId = projects.First().Id;

            var url = $"/heappe/Management/Project?id={projectId}&sessionCode={sessionCode}";
            var project = await _client.GetJsonAsync<ProjectExt>(url);
            
            project.Should().NotBeNull();
            // Bug A: Missing Include for CommandTemplates
            project.CommandTemplates.Should().NotBeNull();
        }

        #endregion

        #region Management ClusterNodeTypes

        [Fact]
        public async Task ManagementClusterNodeTypes_ShouldLoadNavigationProperties()
        {
            var sessionCode = await GetAdminSessionCodeAsync();
            var url = $"/heappe/Management/ClusterNodeTypes?sessionCode={sessionCode}";
            
            var nodeTypes = await _client.GetJsonAsync<List<ClusterNodeTypeExt>>(url);
            
            nodeTypes.Should().NotBeNull();
        }

        #endregion

        #region Management AdaptorUsers

        [Fact]
        public async Task ManagementAdaptorUsers_ShouldLoadNavigationProperties()
        {
            var sessionCode = await GetAdminSessionCodeAsync();
            var url = $"/heappe/Management/AdaptorUsers?sessionCode={sessionCode}";
            
            var users = await _client.GetJsonAsync<List<AdaptorUserExt>>(url);
            
            users.Should().NotBeNull();
            users.Should().NotBeEmpty();
            
            foreach (var user in users)
            {
                // Bug E: Missing Include for AdaptorUserGroups
                user.AdaptorUserGroups.Should().NotBeNull();
            }
        }

        #endregion

        #region Management CommandTemplates

        [Fact]
        public async Task ManagementCommandTemplates_ShouldLoadNavigationProperties()
        {
            var sessionCode = await GetAdminSessionCodeAsync();
            var projectsUrl = $"/heappe/Management/Projects?sessionCode={sessionCode}";
            var projects = await _client.GetJsonAsync<List<ProjectExt>>(projectsUrl);
            projects.Should().NotBeNullOrEmpty();
            var projectId = projects.First().Id;

            var url = $"/heappe/Management/CommandTemplates?projectId={projectId}&sessionCode={sessionCode}";
            
            var templates = await _client.GetJsonAsync<List<CommandTemplateExt>>(url);
            
            templates.Should().NotBeNull();
            
            foreach (var template in templates)
            {
                template.TemplateParameters.Should().NotBeNull();
            }
        }

        #endregion

        #region ClusterInformation

        [Fact]
        public async Task ListAvailableClusters_ShouldLoadNavigationProperties()
        {
            var sessionCode = await GetAdminSessionCodeAsync();
            var url = $"/heappe/ClusterInformation/ListAvailableClusters?sessionCode={sessionCode}";
            
            var clusters = await _client.GetJsonAsync<IEnumerable<ClusterExt>>(url);
            
            clusters.Should().NotBeNull();
            clusters.Should().NotBeEmpty();
            
            foreach (var cluster in clusters)
            {
                cluster.NodeTypes.Should().NotBeNull();
                cluster.FileTransferMethodIds.Should().NotBeNull();
            }
        }
        #endregion
    }
}
