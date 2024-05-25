﻿using HEAppE.RestApiModels.AbstractModels;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace HEAppE.RestApiModels.Management
{
    [DataContract(Name = "CreateClusterNodeTypeModel")]
    public class CreateClusterNodeTypeModel : SessionCodeModel
    {
        [DataMember(Name = "Name", IsRequired = true), StringLength(50)]
        public string Name { get; set; }

        [DataMember(Name = "Description", IsRequired = false), StringLength(200)]
        public string Description { get; set; }

        [DataMember(Name = "NumberOfNodes")]
        public int? NumberOfNodes { get; set; }

        [DataMember(Name = "CoresPerNode", IsRequired = true)]
        public int CoresPerNode { get; set; }

        [DataMember(Name = "Queue"), StringLength(30)]
        public string Queue { get; set; }

        [DataMember(Name = "QualityOfService"), StringLength(40)]
        public string QualityOfService { get; set; }

        [DataMember(Name = "MaxWalltime")]
        public int? MaxWalltime { get; set; }

        [DataMember(Name = "ClusterAllocationName"), StringLength(40)]
        public string ClusterAllocationName { get; set; }

        [DataMember(Name = "ClusterId")]
        public long? ClusterId { get; set; }

        [DataMember(Name = "FileTransferMethodId")]
        public long? FileTransferMethodId { get; set; }

        [DataMember(Name = "ClusterNodeTypeAggregationId")]
        public long? ClusterNodeTypeAggregationId { get; set; }
    }
}
