using HEAppE.DomainObjects.ClusterInformation;
using System;

namespace HEAppE.HpcConnectionFramework.SchedulerAdapters
{
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
        #region Constructors
        public SchedulerEndpoint(string masterNodeName, SchedulerType schedulerType)
        {
            MasterNodeName = masterNodeName;
            SchedulerType = schedulerType;
        }
        #endregion
        #region Override Methods
        public override bool Equals(object obj)
        {
            return obj is SchedulerEndpoint endpoint &&
                   MasterNodeName.Equals(endpoint.MasterNodeName) &&
                   SchedulerType.Equals(endpoint.SchedulerType);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(MasterNodeName, SchedulerType);
        }
        #endregion
    }
}