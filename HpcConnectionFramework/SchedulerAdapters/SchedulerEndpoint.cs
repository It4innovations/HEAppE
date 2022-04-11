using HEAppE.DomainObjects.ClusterInformation;
using System;

namespace HEAppE.HpcConnectionFramework.SchedulerAdapters
{
    /// <summary>
    /// Scheduler endpoint
    /// </summary>
    internal struct SchedulerEndpoint
    {
        #region Properties
        /// <summary>
        /// Master nodename (host)
        /// </summary>
        public string MasterNodeName { get; private set; }

        /// <summary>
        /// Scheduler type
        /// </summary>
        public SchedulerType SchedulerType { get; private set; }
        #endregion
        #region Constructors,
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="masterNodeName">Master node name</param>
        /// <param name="schedulerType">Scheduler type</param>
        public SchedulerEndpoint(string masterNodeName, SchedulerType schedulerType)
        {
            MasterNodeName = masterNodeName;
            SchedulerType = schedulerType;
        }
        #endregion
        #region Override Methods
        /// <summary>
        /// Equals 
        /// </summary>
        /// <param name="obj">Object</param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            return obj is SchedulerEndpoint endpoint &&
                   MasterNodeName.Equals(endpoint.MasterNodeName) &&
                   SchedulerType.Equals(endpoint.SchedulerType);
        }

        /// <summary>
        /// Get hash code
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return HashCode.Combine(MasterNodeName, SchedulerType);
        }
        #endregion
    }
}