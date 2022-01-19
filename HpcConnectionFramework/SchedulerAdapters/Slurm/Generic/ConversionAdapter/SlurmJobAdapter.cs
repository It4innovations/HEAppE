using HEAppE.DomainObjects.JobManagement.JobInformation;
using HEAppE.HpcConnectionFramework.ConversionAdapter;
using HEAppE.HpcConnectionFramework.SchedulerAdapters.Slurm.DTO;
using System;
using System.Collections.Generic;
using System.Text;

namespace HEAppE.HpcConnectionFramework.SchedulerAdapters.Slurm.Generic.ConversionAdapter
{
    /// <summary>
    /// Class: Slurm job adapter
    /// </summary>
    internal class SlurmJobAdapter : ISchedulerJobAdapter
    {
        #region Properties
        /// <summary>
        /// Job command builder (create job)
        /// </summary>
        protected StringBuilder _jobCommandBuilder;

        /// <summary>
        /// Job parameter (result of job)
        /// </summary>
        protected SlurmJobDTO _jobParameters;

        /// <summary>
        /// Job priority multiplier for setting priority from range [0-max(int)]
        /// </summary>
        protected static int _priorityMultiplier = 25000;
        #endregion
        #region Constructors
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="jobSource"></param>
        public SlurmJobAdapter(string jobSource)
        {
            //TODO divide create and read job parameters
            _jobCommandBuilder = new StringBuilder(jobSource);
            _jobParameters = SlurmConversionUtils.ReadParametersFromSqueueResponse(jobSource);
        }
        #endregion
        #region ISchedulerJobAdapter Members
        /// <summary>
        /// Job command
        /// </summary>
        public object Source
        {
            get { return _jobCommandBuilder.ToString(); }
        }

        /// <summary>
        /// Scheduled job id
        /// </summary>
        public string Id
        {
            get { return _jobParameters.Id.ToString(); }
        }

        /// <summary>
        /// Job name
        /// Note: Name of the job is set in the appropriate task.
        /// </summary>
        public string Name
        {
            get { return _jobParameters.Name; }
            set { }
        }

        /// <summary>
        /// Job project
        /// Note: Slurm does not have project for job
        /// </summary>
        public virtual string Project
        {
            get { return string.Empty; }
            set { }
        }

        /// <summary>
        /// Job account
        /// Note: Slurm does not have Accounting string
        /// </summary>
        public string AccountingString
        {
            get { return string.Empty; }
            set { }
        }

        /// <summary>
        /// Job state
        /// </summary>
        public virtual JobState State
        {
            get { return _jobParameters.State; }
        }

        /// <summary>
        /// Job creational time
        /// </summary>
        public DateTime CreateTime
        {
            get { return _jobParameters.CreationTime; }
        }

        /// <summary>
        /// Job submition time
        /// </summary>
        public DateTime? SubmitTime
        {
            get { return _jobParameters.SubmitTime; }
        }

        /// <summary>
        /// Job start time
        /// </summary>
        public DateTime? StartTime
        {
            get { return _jobParameters.StartTime; }
        }

        /// <summary>
        /// Job end time
        /// </summary>
        public virtual DateTime? EndTime
        {
            get { return _jobParameters.EndTime; }
        }


        /// <summary>
        /// Job runtime
        /// Note: Runtime is used in tasks - used Allocated time 
        /// </summary>
        public int Runtime
        {
            get { return (int)_jobParameters.AllocatedTime.TotalSeconds; }
            set { }
        }

        /// <summary>
        /// Method: Get task list for job
        /// </summary>
        /// <returns></returns>
        public List<object> GetTaskList()
        {
            return new List<object>
            {
                _jobCommandBuilder.ToString()
            };
        }


        /// <summary>
        /// Method: Create empty task
        /// </summary>
        /// <returns></returns>
        public object CreateEmptyTaskObject()
        {
            return string.Empty;
        }

        /// <summary>
        /// Method: Set notification for job
        /// </summary>
        /// <param name="mailAddress">Mail address</param>
        /// <param name="notifyOnStart">Notification on start job</param>
        /// <param name="notifyOnCompletion">Notification on completed job</param>
        /// <param name="notifyOnFailure">Notification on failured job</param>
        public void SetNotifications(string mailAddress, bool? notifyOnStart, bool? notifyOnCompletion, bool? notifyOnFailure)
        {
            if (!string.IsNullOrEmpty(mailAddress)
                    && (notifyOnStart ?? false)
                        || (notifyOnCompletion ?? false)
                        || (notifyOnFailure ?? false))
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

        /// <summary>
        /// Method: Set tasks for job
        /// Note: When multiple jobs are implemented, the SetTasks method has to be changed
        /// </summary>
        /// <param name="tasks">Tasks</param>
        public void SetTasks(List<object> tasks)
        {
            foreach (var task in tasks)
            {
                _jobCommandBuilder.Append((string)task);
            }
        }
        #endregion
    }
}
