using HEAppE.DomainObjects.UserAndLimitationManagement;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Xml.Linq;

namespace HEAppE.ExtModels.UserAndLimitationManagement.Models
{
    [DataContract(Name = "ClusterNodeTypeResourceUsageExt")]
    public class ClusterNodeTypeResourceUsageExt
    {
        [DataMember(Name = "Id")]
        public long? Id { get; set; }

        [DataMember(Name = "Name")]
        public string Name { get; set; }

        [DataMember(Name = "Description")]
        public string Description { get; set; }

        [DataMember(Name = "NumberOfNodes")]
        public int? NumberOfNodes { get; set; }

        [DataMember(Name = "CoresPerNode")]
        public int? CoresPerNode { get; set; }

        [DataMember(Name = "MaxWalltime")]
        public int? MaxWalltime { get; set; }

        [DataMember(Name = "FileTransferMethodId")]
        public long? FileTransferMethodId { get; set; }

        [DataMember(Name = "NodeUsedCoresAndLimitation")]
        public NodeUsedCoresAndLimitationExt NodeUsedCoresAndLimitation { get; set; }

        #region Public methods
        public override string ToString()
        {
            return $"ReportingClusterNodeTypeExt: Id={Id}, Name={Name}, Description={Description}, numberOfNodes={NumberOfNodes}, coresPerNode={CoresPerNode}, MaxWalltime={MaxWalltime}, FileTransferMethodId={FileTransferMethodId}, NodeUsedCoresAndLimitation={NodeUsedCoresAndLimitation}";
        }
        #endregion
    }
}