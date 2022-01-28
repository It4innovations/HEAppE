using System.Collections.Generic;
using System.Linq;
using System.Text;
using HEAppE.DomainObjects.JobManagement;
using HEAppE.HpcConnectionFramework.SchedulerAdapters.ConversionAdapter;

namespace HEAppE.HpcConnectionFramework.SchedulerAdapters.PbsPro.Generic.ConversionAdapter
{
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
        /// Set notification for job
        /// </summary>
        /// <param name="mailAddress">Mail address</param>
        /// <param name="notifyOnStart">Notification on start job</param>
        /// <param name="notifyOnCompletion">Notification on completed job</param>
        /// <param name="notifyOnFailure">Notification on failured job</param>
        public void SetNotifications(string mailAddress, bool? notifyOnStart, bool? notifyOnCompletion, bool? notifyOnFailure)
        {
            if (!string.IsNullOrEmpty(mailAddress) && (notifyOnStart ?? false)
                    || (notifyOnCompletion ?? false) || (notifyOnFailure ?? false))
            {
                var onFailureValue = (notifyOnFailure ?? false ? "a" : string.Empty);
                var onStartValue = (notifyOnStart ?? false ? "b" : string.Empty);
                var onCompletionValue = (notifyOnCompletion ?? false ? "e" : string.Empty);
                _jobCommandBuilder.Append(@$" -M {mailAddress} -m {onFailureValue}{onStartValue}{onCompletionValue}");
            }
        }

        /// <summary>
        /// Set tasks for job
        /// </summary>
        /// <param name="tasks">Tasks</param>
        public void SetTasks(IEnumerable<object> tasks)
        {
            _jobCommandBuilder.Clear();
            foreach (var task in tasks)
            {
                _jobCommandBuilder.Append((string)task);
            }
        }
        #endregion
    }
}