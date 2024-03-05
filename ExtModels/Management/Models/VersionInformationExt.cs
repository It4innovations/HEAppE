using HEAppE.ExtModels.JobManagement.Models;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace HEAppE.ExtModels.Management.Models
{
    /// <summary>
    /// Instance information Ext
    /// </summary>
    [DataContract(Name = "VersionInformationExt")]
    public class VersionInformationExt
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
        #endregion
        #region Override methods
        /// <summary>
        /// Override to string method
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"InstanceInformationExt: Name={Name}, Description={Description}, Version={Version}";
        }
        #endregion
    }
}