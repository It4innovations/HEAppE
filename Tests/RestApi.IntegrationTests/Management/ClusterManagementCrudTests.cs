using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using FluentAssertions;
using HEAppE.DomainObjects.ClusterInformation;
using HEAppE.DomainObjects.FileTransfer;
using HEAppE.ExtModels.ClusterInformation.Models;
using HEAppE.ExtModels.FileTransfer.Models;
using HEAppE.ExtModels.Management.Models;
using HEAppE.RestApi.IntegrationTests.Infrastructure;
using HEAppE.RestApiModels.Management;
using Xunit;

namespace HEAppE.RestApi.IntegrationTests.Management;

[Trait("Category", "Integration")]
public class ClusterManagementCrudTests : ManagementTestBase
{
    public ClusterManagementCrudTests(HEAppEWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task Cluster_NodeType_And_Proxy_FullCrudLifecycle_Succeeds()
    {
        var sessionCode = await GetAdminSessionCodeAsync();
        var uniqueSuffix = Guid.NewGuid().ToString("N")[..8];
        var clusterName = $"Clust-{uniqueSuffix}";
        var masterNodeName = $"node-{uniqueSuffix}.ci";

        // 1. CREATE Cluster
        var createClusterModel = new CreateClusterModel
        {
            Name = clusterName,
            Description = "Integration test cluster",
            MasterNodeName = masterNodeName,
            SchedulerType = SchedulerTypeExt.Slurm,
            ConnectionProtocol = ClusterConnectionProtocolExt.Ssh,
            TimeZone = "UTC",
            Port = 22,
            UpdateJobStateByServiceAccount = true,
            DomainName = $"{uniqueSuffix}.ci",
            ProxyConnectionId = null,
            SessionCode = sessionCode
        };

        var createdCluster = await _client.PostJsonAsync<CreateClusterModel, ClusterExt>(
            "/heappe/Management/Cluster", createClusterModel);
        createdCluster.Should().NotBeNull();
        createdCluster.Id.Should().NotBeNull();
        createdCluster.Id.Value.Should().BeGreaterThan(0);
        createdCluster.Name.Should().Be(clusterName);

        var clusterId = createdCluster.Id.Value;

        try
        {
            // 2. READ Cluster
            var fetchedCluster = await _client.GetJsonAsync<ClusterExt>(
                $"/heappe/Management/Cluster?id={clusterId}&sessionCode={sessionCode}");
            fetchedCluster.Should().NotBeNull();
            fetchedCluster.Id.Should().Be(clusterId);
            fetchedCluster.Name.Should().Be(clusterName);

            // 3. LIST Clusters
            var allClusters = await _client.GetJsonAsync<List<ClusterExt>>(
                $"/heappe/Management/Clusters?sessionCode={sessionCode}");
            allClusters.Should().NotBeNull();
            allClusters.Should().Contain(c => c.Id == clusterId);

            // 4. UPDATE Cluster
            var updatedClusterName = $"Updated-{clusterName}";
            var modifyClusterModel = new ModifyClusterModel
            {
                Id = clusterId,
                Name = updatedClusterName,
                Description = "Updated cluster description",
                MasterNodeName = masterNodeName,
                SchedulerType = SchedulerTypeExt.Slurm,
                ConnectionProtocol = ClusterConnectionProtocolExt.Ssh,
                TimeZone = "UTC",
                Port = 22,
                UpdateJobStateByServiceAccount = true,
                DomainName = $"{uniqueSuffix}.ci",
                ProxyConnectionId = null,
                SessionCode = sessionCode
            };

            var modifiedCluster = await _client.PutJsonAsync<ModifyClusterModel, ClusterExt>(
                "/heappe/Management/Cluster", modifyClusterModel);
            modifiedCluster.Should().NotBeNull();
            modifiedCluster.Name.Should().Be(updatedClusterName);

            // 5. CREATE ClusterNodeType
            var nodeTypeName = $"Node-{uniqueSuffix}";
            var createNodeTypeModel = new CreateClusterNodeTypeModel
            {
                Name = nodeTypeName,
                Description = "Test node type",
                NumberOfNodes = 4,
                CoresPerNode = 16,
                Queue = "standard",
                QualityOfService = "normal",
                MaxWalltime = 3600,
                ClusterId = clusterId,
                SessionCode = sessionCode
            };

            var createdNodeType = await _client.PostJsonAsync<CreateClusterNodeTypeModel, ClusterNodeTypeExt>(
                "/heappe/Management/ClusterNodeType", createNodeTypeModel);
            createdNodeType.Should().NotBeNull();
            createdNodeType.Id.Should().NotBeNull();
            createdNodeType.Id.Value.Should().BeGreaterThan(0);
            createdNodeType.Name.Should().Be(nodeTypeName);

            var nodeTypeId = createdNodeType.Id.Value;

            // 6. READ ClusterNodeType
            var fetchedNodeType = await _client.GetJsonAsync<ClusterNodeTypeExt>(
                $"/heappe/Management/ClusterNodeType?id={nodeTypeId}&sessionCode={sessionCode}");
            fetchedNodeType.Should().NotBeNull();
            fetchedNodeType.Id.Should().Be(nodeTypeId);

            // 7. LIST ClusterNodeTypes
            var allNodeTypes = await _client.GetJsonAsync<List<ClusterNodeTypeExt>>(
                $"/heappe/Management/ClusterNodeTypes?sessionCode={sessionCode}");
            allNodeTypes.Should().NotBeNull();
            allNodeTypes.Should().Contain(n => n.Id == nodeTypeId);

            // 8. UPDATE ClusterNodeType
            var updatedNodeTypeName = $"Updated-{nodeTypeName}";
            var modifyNodeTypeModel = new ModifyClusterNodeTypeModel
            {
                Id = nodeTypeId,
                Name = updatedNodeTypeName,
                Description = "Updated node type",
                NumberOfNodes = 8,
                CoresPerNode = 32,
                Queue = "standard",
                QualityOfService = "high",
                MaxWalltime = 7200,
                ClusterId = clusterId,
                SessionCode = sessionCode
            };

            var modifiedNodeType = await _client.PutJsonAsync<ModifyClusterNodeTypeModel, ClusterNodeTypeExt>(
                "/heappe/Management/ClusterNodeType", modifyNodeTypeModel);
            modifiedNodeType.Should().NotBeNull();
            modifiedNodeType.Name.Should().Be(updatedNodeTypeName);

            // 9. DELETE ClusterNodeType
            var removeNodeTypeModel = new RemoveClusterNodeTypeModel
            {
                Id = nodeTypeId,
                SessionCode = sessionCode
            };
            var deleteNodeTypeResp = await _client.DeleteJsonAsync(
                "/heappe/Management/ClusterNodeType", removeNodeTypeModel);
            deleteNodeTypeResp.StatusCode.Should().Be(HttpStatusCode.OK);

            // 10. PROXY CONNECTION CRUD
            var proxyPassword = TestCredentials.GenerateRandomPassword();
            var createProxyModel = new CreateClusterProxyConnectionModel
            {
                Host = "proxy.ci.test",
                Port = 1080,
                Username = "proxyuser",
                Password = proxyPassword,
                Type = ProxyType.Socks5,
                SessionCode = sessionCode
            };

            var createdProxy = await _client.PostJsonAsync<CreateClusterProxyConnectionModel, ClusterProxyConnectionExt>(
                "/heappe/Management/ClusterProxyConnection", createProxyModel);
            createdProxy.Should().NotBeNull();
            createdProxy.Id.Should().BeGreaterThan(0);

            var proxyId = createdProxy.Id;

            // Read Proxy
            var fetchedProxy = await _client.GetJsonAsync<ClusterProxyConnectionExt>(
                $"/heappe/Management/ClusterProxyConnection?id={proxyId}&sessionCode={sessionCode}");
            fetchedProxy.Should().NotBeNull();
            fetchedProxy.Id.Should().Be(proxyId);

            // List Proxies
            var allProxies = await _client.GetJsonAsync<List<ClusterProxyConnectionExt>>(
                $"/heappe/Management/ClusterProxyConnections?sessionCode={sessionCode}");
            allProxies.Should().NotBeNull();
            allProxies.Should().Contain(p => p.Id == proxyId);

            // Modify Proxy
            var modifyProxyModel = new ModifyClusterProxyConnectionModel
            {
                Id = proxyId,
                Host = "updated-proxy.ci.test",
                Port = 1081,
                Username = "proxyuser",
                Password = proxyPassword,
                Type = ProxyType.Socks5,
                SessionCode = sessionCode
            };
            var modifiedProxy = await _client.PutJsonAsync<ModifyClusterProxyConnectionModel, ClusterProxyConnectionExt>(
                "/heappe/Management/ClusterProxyConnection", modifyProxyModel);
            modifiedProxy.Should().NotBeNull();
            modifiedProxy.Host.Should().Be("updated-proxy.ci.test");

            // Delete Proxy
            var removeProxyModel = new RemoveClusterProxyConnectionModel
            {
                Id = proxyId,
                SessionCode = sessionCode
            };
            var deleteProxyResp = await _client.DeleteJsonAsync(
                "/heappe/Management/ClusterProxyConnection", removeProxyModel);
            deleteProxyResp.StatusCode.Should().Be(HttpStatusCode.OK);

            // 11. FILE TRANSFER METHODS (Read seeded)
            var ftmList = await _client.GetJsonAsync<List<FileTransferMethodExt>>(
                $"/heappe/Management/FileTransferMethods?sessionCode={sessionCode}");
            ftmList.Should().NotBeNull();
            if (ftmList.Count > 0)
            {
                var singleFtm = await _client.GetJsonAsync<FileTransferMethodExt>(
                    $"/heappe/Management/FileTransferMethod?id={ftmList[0].Id}&sessionCode={sessionCode}");
                singleFtm.Should().NotBeNull();
                singleFtm.Id.Should().Be(ftmList[0].Id);
            }

            // 12. FILE TRANSFER METHOD WRITE CRUD
            var createFtmModel = new CreateFileTransferMethodModel
            {
                ServerHostname = "storage.ci.test",
                Protocol = FileTransferProtocol.SftpScp,
                ClusterId = clusterId,
                Port = 22,
                SessionCode = sessionCode
            };
            var createdFtm = await _client.PostJsonAsync<CreateFileTransferMethodModel, FileTransferMethodNoCredentialsExt>(
                "/heappe/Management/FileTransferMethod", createFtmModel);
            createdFtm.Should().NotBeNull();
            createdFtm.Id.Should().BeGreaterThan(0);
            var ftmId = createdFtm.Id;

            var modifyFtmModel = new ModifyFileTransferMethodModel
            {
                Id = ftmId,
                ServerHostname = "updated-storage.ci.test",
                Protocol = FileTransferProtocol.SftpScp,
                ClusterId = clusterId,
                Port = 2222,
                SessionCode = sessionCode
            };
            var modifiedFtm = await _client.PutJsonAsync<ModifyFileTransferMethodModel, FileTransferMethodNoCredentialsExt>(
                "/heappe/Management/FileTransferMethod", modifyFtmModel);
            modifiedFtm.Should().NotBeNull();
            modifiedFtm.ServerHostname.Should().Be("updated-storage.ci.test");

            var removeFtmModel = new RemoveFileTransferMethodModel
            {
                Id = ftmId,
                SessionCode = sessionCode
            };
            var deleteFtmResp = await _client.DeleteJsonAsync("/heappe/Management/FileTransferMethod", removeFtmModel);
            deleteFtmResp.StatusCode.Should().Be(HttpStatusCode.OK);
        }
        finally
        {
            // 12. DELETE Cluster
            var removeClusterModel = new RemoveClusterModel
            {
                Id = clusterId,
                SessionCode = sessionCode
            };
            var deleteClusterResp = await _client.DeleteJsonAsync("/heappe/Management/Cluster", removeClusterModel);
            deleteClusterResp.StatusCode.Should().Be(HttpStatusCode.OK);

            // Verify deleted
            var getDeletedResp = await _client.GetAsync($"/heappe/Management/Cluster?id={clusterId}&sessionCode={sessionCode}");
            getDeletedResp.StatusCode.Should().Match(sc => sc == HttpStatusCode.NotFound || sc == HttpStatusCode.BadRequest);
        }
    }
}
