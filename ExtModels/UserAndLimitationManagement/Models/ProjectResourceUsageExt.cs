using System.Runtime.Serialization;
using System.Xml.Linq;
using System;
using HEAppE.ExtModels.ClusterInformation.Models;
using HEAppE.DomainObjects.JobManagement;

namespace HEAppE.ExtModels.UserAndLimitationManagement.Models
{
    [DataContract(Name = "ProjectResourceUsageExt")]
    public class ProjectResourceUsageExt
    {
        [DataMember(Name = "Id")]
        public long Id { get; set; }

        [DataMember(Name = "Name")]
        public string Name { get; set; }

        [DataMember(Name = "Description")]
        public string Description { get; set; }

        [DataMember(Name = "AccountingString")]
        public string AccountingString { get; set; }

        [DataMember(Name = "StartDate")]
        public DateTime StartDate { get; set; }

        [DataMember(Name = "EndDate")]
        public DateTime EndDate { get; set; }

        [DataMember(Name = "EndDate")]
        public ClusterNodeTypeResourceUsageExt[] NodeTypes { get; set; }

        #region Public methods
        public override string ToString()
        {
            return $"ResourceProjectExt: Id={Id}, Name={Name}, Description={Description}, AccountingString={AccountingString}, StartDate={StartDate}, EndDate={EndDate}, NodeTypes={NodeTypes}";
        }
        #endregion
    }
}