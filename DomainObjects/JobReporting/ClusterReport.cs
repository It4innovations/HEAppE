﻿using System;
using System.Collections.Generic;
using System.Linq;
using HEAppE.DomainObjects.ClusterInformation;

namespace HEAppE.DomainObjects.JobReporting;

public class ClusterReport
{
    public Cluster Cluster { get; set; }
    public List<ClusterNodeTypeReport> ClusterNodeTypes { get; set; }
    public double? TotalUsage => Math.Round(ClusterNodeTypes.Sum(x => x.TotalUsage) ?? 0, 3);
}