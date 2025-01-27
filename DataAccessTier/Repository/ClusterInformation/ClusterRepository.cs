﻿using System;
using System.Collections.Generic;
using System.Linq;
using HEAppE.DataAccessTier.IRepository.ClusterInformation;
using HEAppE.DomainObjects.ClusterInformation;
using HEAppE.DomainObjects.JobManagement;

namespace HEAppE.DataAccessTier.Repository.ClusterInformation;

internal class ClusterRepository : GenericRepository<Cluster>, IClusterRepository
{
    #region Constructors

    internal ClusterRepository(MiddlewareContext context)
        : base(context)
    {
    }

    #endregion

    #region Public Methods

    /// <summary>
    ///     Get all clusters with cluster nodes and defined command templates only with active project
    /// </summary>
    /// <returns></returns>
    public IEnumerable<Cluster> GetAllWithActiveProjectFilter(IEnumerable<Project> projects)
    {
        return _dbSet.ToList().Select(c => GetCluster(c, projects)).ToList();
    }

    /// <summary>
    ///     Get all clusters with Cluster Proxy Connection id
    /// </summary>
    /// <param name="clusterProxyConnectionId"></param>
    /// <returns></returns>
    public IEnumerable<Cluster> GetAllByClusterProxyConnectionId(long clusterProxyConnectionId)
    {
        return _dbSet.Where(c => c.ProxyConnectionId == clusterProxyConnectionId).ToList();
    }

    #endregion

    #region Private Methods

    private Cluster GetCluster(Cluster cluster, IEnumerable<Project> projects)
    {
        return new Cluster
        {
            Id = cluster.Id,
            Name = cluster.Name,
            Description = cluster.Description,
            ClusterProjects = cluster.ClusterProjects,
            ConnectionProtocol = cluster.ConnectionProtocol,
            DomainName = cluster.DomainName,
            FileTransferMethods = cluster.FileTransferMethods.ToList(),
            MasterNodeName = cluster.MasterNodeName,
            Port = cluster.Port,
            ProxyConnection = cluster.ProxyConnection == null ? null : cluster.ProxyConnection,
            ProxyConnectionId = cluster.ProxyConnection == null ? null : cluster.ProxyConnectionId,
            SchedulerType = cluster.SchedulerType,
            TimeZone = cluster.TimeZone,
            UpdateJobStateByServiceAccount = cluster.UpdateJobStateByServiceAccount,
            NodeTypes = cluster.NodeTypes.Select(n => GetClusterNodeType(n, projects)).ToList()
        };
    }

    private ClusterNodeType GetClusterNodeType(ClusterNodeType n, IEnumerable<Project> projects)
    {
        return new ClusterNodeType
        {
            Id = n.Id,
            Name = n.Name,
            ClusterId = n.ClusterId,
            Cluster = n.Cluster,
            ClusterAllocationName = n.ClusterAllocationName,
            CoresPerNode = n.CoresPerNode,
            Description = n.Description,
            FileTransferMethod = n.FileTransferMethod == null ? null : n.FileTransferMethod,
            FileTransferMethodId = n.FileTransferMethod == null ? null : n.FileTransferMethodId,
            MaxWalltime = n.MaxWalltime,
            NumberOfNodes = n.NumberOfNodes,
            Queue = n.Queue,
            RequestedNodeGroups = n.RequestedNodeGroups,
            PossibleCommands = n.PossibleCommands
                .Where(p => p.ProjectId == null || p.Project?.EndDate >= DateTime.UtcNow).ToList()
        };
    }

    #endregion
}