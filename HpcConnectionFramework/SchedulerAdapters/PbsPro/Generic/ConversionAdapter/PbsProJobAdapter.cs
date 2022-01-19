using System;
using System.Collections.Generic;
using System.Text;
using HEAppE.DomainObjects.JobManagement;
using HEAppE.DomainObjects.JobManagement.JobInformation;
using HEAppE.HpcConnectionFramework.ConversionAdapter;

namespace HEAppE.HpcConnectionFramework.SchedulerAdapters.PbsPro.Generic.ConversionAdapter
{
    public class PbsProJobAdapter : ISchedulerJobAdapter
    {
        #region Constructors
        public PbsProJobAdapter(object jobSource)
        {
            this.jobSource = (string)jobSource;
            qstatInfo = PbsProConversionUtils.ReadQstatResultFromJobSource(this.jobSource);
        }
        #endregion

        #region ISchedulerJobAdapter Members
        public object Source
        {
            get { return jobSource; }
        }

        public string Id
        {
            get
            {
                string result;
                if (qstatInfo.TryGetValue(PbsProJobInfoAttributes.JOB_ID, out result))
                    return PbsProConversionUtils.GetJobIdFromJobCode(result);
                return "";
            }
        }

        public string Name
        {
            get
            {
                string result;
                if (qstatInfo.TryGetValue(PbsProJobInfoAttributes.JOB_NAME, out result))
                    return result;
                return string.Empty;
            }
            ///<summary>Name of the job is set in the appropriate task.</summary>
            set { }
        }

        public virtual string Project
        {
            get
            {
                // Project is not supported in this PBS version
                return string.Empty;
            }
            set
            {
                // Project is not supported in this PBS version
            }
        }

        public string AccountingString
        {
            get
            {
                string result;
                if (qstatInfo.TryGetValue(PbsProJobInfoAttributes.ACCOUNT_NAME, out result))
                    return result;
                return string.Empty;
            }
            set
            {
                if (!string.IsNullOrEmpty(value))
                    jobSource += " -A " + value;
            }
        }

        public virtual JobState State
        {
            get
            {
                string result;
                if (qstatInfo.TryGetValue(PbsProJobInfoAttributes.JOB_STATE, out result))
                    return ConvertPbsJobStateToIndependentJobState(result);
                return JobState.Finished;
            }
        }

        public DateTime CreateTime
        {
            get
            {
                string result;
                if (qstatInfo.TryGetValue(PbsProJobInfoAttributes.CTIME, out result))
                    return PbsProConversionUtils.ConvertQstatDateStringToDateTime(result);
                return DateTime.UtcNow;
            }
        }

        public DateTime? SubmitTime
        {
            get
            {
                string result;
                if (qstatInfo.TryGetValue(PbsProJobInfoAttributes.ETIME, out result))
                    return PbsProConversionUtils.ConvertQstatDateStringToDateTime(result);
                return null;
            }
        }

        public DateTime? StartTime
        {
            get
            {
                string result;
                if (qstatInfo.TryGetValue(PbsProJobInfoAttributes.STIME, out result))
                    return PbsProConversionUtils.ConvertQstatDateStringToDateTime(result);
                return null;
            }
        }

        public virtual DateTime? EndTime
        {
            /// <summary>EndTime is not supported in the Linux PBS Scheduler.</summary>
            get { return null; }
        }


        public int Runtime
        {
            get
            {
                string result;
                if (qstatInfo.TryGetValue(PbsProJobInfoAttributes.RESOURCE_LIST_WALLTIME, out result))
                {
                    return PbsProConversionUtils.ConvertQstatTimeStringToSeconds(result);
                }
                return 0;
            }
            set
            {
                /*if (value > 0)
					jobSource += " -l walltime=" + LinuxPbsConversionUtils.ConvertSecondsToQstatTimeString(value);*/
            }
        }

        public List<object> GetTaskList()
        {
            List<object> tasks = new List<object>();
            tasks.Add(jobSource);
            return tasks;
        }

        /// <summary>Resources are set in tasks.</summary>

        public object CreateEmptyTaskObject()
        {
            return string.Empty;
        }

        public void SetNotifications(string mailAddress, bool? notifyOnStart, bool? notifyOnCompletion, bool? notifyOnFailure)
        {
            if (!string.IsNullOrEmpty(mailAddress))
            {
                jobSource += " -M " + mailAddress;
                if ((notifyOnStart ?? false) || (notifyOnCompletion ?? false) || (notifyOnFailure ?? false))
                    jobSource += " -m ";
                if (notifyOnFailure ?? false)
                    jobSource += "a";
                if (notifyOnStart ?? false)
                    jobSource += "b";
                if (notifyOnCompletion ?? false)
                    jobSource += "e";
            }
        }

        public virtual void SetTasks(List<object> tasks)
        {
#warning When multiple jobs are implemented, the SetTasks method has to be changed
            foreach (object task in tasks)
                jobSource = (string)task;
        }

        public void SetEnvironmentVariablesToJob(ICollection<EnvironmentVariable> variables)
        {
            if (variables != null && variables.Count > 0)
            {
                StringBuilder builder = new StringBuilder(" -v ");
                bool first = true;
                foreach (EnvironmentVariable variable in variables)
                {
                    if (first)
                        first = false;
                    else
                        builder.Append(",");
                    builder.Append(variable.Name);
                    builder.Append("=");
                    builder.Append(variable.Value);
                }
                jobSource += builder.ToString();
            }
        }
        #endregion

        #region Local Methods
        protected virtual JobState ConvertPbsJobStateToIndependentJobState(string jobState)
        {
            if (jobState == "H")
                return JobState.Configuring;
            if (jobState == "W")
                return JobState.Submitted;
            if (jobState == "Q" || jobState == "T")
                return JobState.Queued;
            if (jobState == "R" || jobState == "U" || jobState == "S" || jobState == "E")
                return JobState.Running;
            throw new ApplicationException("Job state \"" + jobState +
                                           "\" could not be converted to any known job state.");
        }
        #endregion

        #region Instance Fields
        protected string jobSource;
        protected Dictionary<string, string> qstatInfo;
        #endregion
    }
}