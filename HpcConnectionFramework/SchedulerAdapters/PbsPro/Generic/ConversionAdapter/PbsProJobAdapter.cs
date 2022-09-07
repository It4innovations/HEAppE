using HEAppE.HpcConnectionFramework.SchedulerAdapters.ConversionAdapter;
using System.Collections.Generic;
using System.Text;

namespace HEAppE.HpcConnectionFramework.SchedulerAdapters.PbsPro.Generic.ConversionAdapter
{
    /// <summary>
    /// PBS Professional job adapter
    /// </summary>
    public class PbsProJobAdapter : ISchedulerJobAdapter
    {
        #region Instances
        /// <summary>
        /// Job command builder (create allocation cmd)
        /// </summary>
        protected StringBuilder _jobCommandBuilder;
        #endregion
        #region Constructors
        /// <summary>
        /// Constructor
        /// </summary>
        public PbsProJobAdapter()
        {
            _jobCommandBuilder = new StringBuilder();
        }
        #endregion
        #region ISchedulerJobAdapter Members
        /// <summary>
        /// Job Allocation command
        /// </summary>
        public object AllocationCmd
        {
            get
            {
                return _jobCommandBuilder.ToString();
            }
        }

        /// <summary>
        /// Set tasks for job
        /// </summary>
        /// <param name="tasksAllocationcmd">Tasks</param>
        public void SetTasks(IEnumerable<object> tasksAllocationcmd)
        {
            _jobCommandBuilder.Clear();
            foreach (var task in tasksAllocationcmd)
            {
                _jobCommandBuilder.Append((string)task);
            }
        }

        /// <summary>
        /// Set notification for job
        /// </summary>
        /// <param name="mailAddress">Mail address</param>
        /// <param name="notifyOnStart">Notification on start job</param>
        /// <param name="notifyOnCompletion">Notification on completed job</param>
        /// <param name="notifyOnFailure">Notification on failured job</param>
        public void SetNotifications(string mailAddress, bool? notifyOnStart, bool? notifyOnCompletion, bool? notifyOnFailure)
        {
            if (!string.IsNullOrEmpty(mailAddress) && ((notifyOnStart ?? false)
                               || (notifyOnCompletion ?? false) || (notifyOnFailure ?? false)))
            {
                var onFailureValue = (notifyOnFailure ?? false ? "a" : string.Empty);
                var onStartValue = (notifyOnStart ?? false ? "b" : string.Empty);
                var onCompletionValue = (notifyOnCompletion ?? false ? "e" : string.Empty);
                _jobCommandBuilder.Append(@$" -M {mailAddress} -m {onFailureValue}{onStartValue}{onCompletionValue}");
            }
        }
        #endregion
    }
}