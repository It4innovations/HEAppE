using HEAppE.DomainObjects.ClusterInformation;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace HEAppE.DomainObjects.JobManagement
{
    [Table("ClusterProject")]
    public class ClusterProject
    {
        public long ClusterId { get; set; }
        public virtual Cluster Cluster { get; set; }

        public long ProjectId { get; set; }
        public virtual Project Project { get; set; }
        public virtual List<ClusterProjectCredentials> ClusterProjectCredentials { get; set; } = new List<ClusterProjectCredentials>();
    }
}
