using HEAppE.DomainObjects.ClusterInformation;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HEAppE.DomainObjects.JobManagement
{
    [Table("ClusterProjectCredentials")]
    public class ClusterProjectCredentials
    {
        public long ClusterId { get; set; }
        public long ProjectId { get; set; }
        public virtual ClusterProject ClusterProject { get; set; }

        public long ClusterAuthenticationCredentialsId { get; set; }
        public virtual ClusterAuthenticationCredentials ClusterAuthenticationCredentials { get; set; }

        public bool IsServiceAccount { get; set; } = false;
    }
}
