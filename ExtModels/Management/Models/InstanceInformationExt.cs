using HEAppE.ExtModels.JobManagement.Models;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace HEAppE.ExtModels.Management.Models
{
    /// <summary>
    /// Instance information Ext
    /// </summary>
    [DataContract(Name = "InstanceInformationExt")]
    public class InstanceInformationExt
    {
        #region Properties
        /// <summary>
        /// Name
        /// </summary>
        [DataMember(Name = "Name")]
        public string Name { get; set; }

        /// <summary>
        /// Description
        /// </summary>
        [DataMember(Name = "Description")]
        public string Description { get; set; }

        /// <summary>
        /// Version
        /// </summary>
        [DataMember(Name = "Version")]
        public string Version { get; set; }

        /// <summary>
        /// Deployed IP address
        /// </summary>
        [DataMember(Name = "DeployedIPAddress")]
        public string DeployedIPAddress { get; set; }

        /// <summary>
        /// Port
        /// </summary>
        [DataMember(Name = "Port")]
        public int Port { get; set; }

        /// <summary>
        /// URL
        /// </summary>
        [DataMember(Name = "URL")]
        public string URL { get; set; }

        /// <summary>
        /// URL Postfix
        /// </summary>
        [DataMember(Name = "URLPostfix")]
        public string URLPostfix { get; set; }

        /// <summary>
        /// Deployment type
        /// </summary>
        [DataMember(Name = "DeploymentType")]
        public DeploymentTypeExt DeploymentType { get; set; }

        /// <summary>
        /// Deployment allocation types
        /// </summary>
        [DataMember(Name = "ResourceAllocationTypes")]
        public IEnumerable<ResourceAllocationTypeExt> ResourceAllocationTypes { get; set; }

        /// <summary>
        /// Instance reference to HPC Projects
        /// </summary>
        [DataMember(Name = "Projects")]
        public IEnumerable<ProjectExt> Projects { get; set; }
        #endregion
        #region Override methods
        /// <summary>
        /// Override to string method
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"InstanceInformationExt: Name={Name}, Description={Description}, Version={Version}, DeployedIPAddress={DeployedIPAddress}, Port={Port}, URL={URL}, URLPostfix={URLPostfix}, DeploymetType={DeploymentType}, ResourceAllocationTypes={ResourceAllocationTypes}, Projects={Projects}";
        }
        #endregion
    }
}