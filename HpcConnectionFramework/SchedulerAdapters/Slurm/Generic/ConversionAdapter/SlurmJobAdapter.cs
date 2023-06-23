using HEAppE.HpcConnectionFramework.SchedulerAdapters.ConversionAdapter;
using System.Collections.Generic;
using System.Text;

namespace HEAppE.HpcConnectionFramework.SchedulerAdapters.Slurm.Generic.ConversionAdapter
{
    /// <summary>
    /// Slurm job adapter
    /// </summary>
    internal class SlurmJobAdapter : ISchedulerJobAdapter
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
        public SlurmJobAdapter()
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
                string mailParameters = string.Empty;
                if (notifyOnFailure ?? false)
                {
                    mailParameters += "FAIL,";
                }

                if (notifyOnStart ?? false)
                {
                    mailParameters += "BEGIN,";
                }

                if (notifyOnCompletion ?? false)
                {
                    mailParameters += "END,";
                }

                mailParameters = mailParameters.Remove((mailParameters.Length - 1), 1);
                _jobCommandBuilder.Append($" --mail-user={mailAddress} --mail-type={mailParameters}");
            }
        }
        #endregion
    }
}
