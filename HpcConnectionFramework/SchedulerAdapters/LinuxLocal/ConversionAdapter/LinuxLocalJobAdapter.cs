using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using HEAppE.DomainObjects.JobManagement.JobInformation;
using HEAppE.HpcConnectionFramework.ConversionAdapter;
using HEAppE.HpcConnectionFramework.LinuxPbs;
using HEAppE.HpcConnectionFramework.LinuxPbs.v10.ConversionAdapter;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace HEAppE.HpcConnectionFramework.SchedulerAdapters.LinuxLocal.ConversionAdapter
{
    public class LinuxLocalJobAdapter : ISchedulerJobAdapter
    {
        #region Constructors

        private dynamic jobInfo;

        public LinuxLocalJobAdapter(object jobSource)
        {
            //string jsonJobSource = jobSource.ToString().Substring(1).Substring(0, jobSource.ToString().LastIndexOf(']') - 1);//TODO edit json structure, refactor
            jobInfo = JsonConvert.DeserializeObject(jobSource.ToString());
        }
        #endregion

        #region ISchedulerJobAdapter Members

        public object Source { get; }
        public string Id { get => jobInfo.Id; }

        public string Name
        {
            get => jobInfo.Name;
            set => jobInfo.Name = value;
        }

        public string Project
        {
            get
            {
                if (jobInfo.Project != null)
                    return jobInfo.Project;
                return string.Empty;
            }
            set
            {

            }
        }

        public JobState State
        {
            get
            {
                switch (jobInfo.State?.ToString())
                {
                    case "H":
                        return JobState.Configuring;
                    case "R":
                        return JobState.Running;
                    case "F":
                        return JobState.Finished;
                    case "S":
                        return JobState.Canceled;
                    default:
                        throw new ApplicationException("Job state could not be converted to any known job state.");
                }
            }
        }

        public DateTime CreateTime
        {
            get
            {
                if (jobInfo.CreateTime == null || jobInfo.CreateTime?.ToString() == "null")
                    throw new ApplicationException("Job CreateTime could not be converted to any known job state.");
                else
                {
                    return jobInfo.CreateTime;
                }
            }
        }

        public DateTime? SubmitTime
        {
            get
            {
                if (jobInfo.SubmitTime == null || string.IsNullOrEmpty(jobInfo.SubmitTime.ToString()))
                    return null;
                else
                {
                    return jobInfo.SubmitTime;
                }
            }
        }

        public DateTime? StartTime
        {
            get
            {
                if (jobInfo.StartTime == null || string.IsNullOrEmpty(jobInfo.StartTime.ToString()))
                    return null;
                else
                {
                    return jobInfo.StartTime;
                }
            }
        }

        public DateTime? EndTime
        {
            get
            {
                if (jobInfo.EndTime == null || string.IsNullOrEmpty(jobInfo.EndTime.ToString()))
                    return null;
                else
                {
                    return jobInfo.EndTime;
                }
            }
        }
        public int Runtime { get; set; }
        public string AccountingString { get; set; }

        public List<object> GetTaskList()
        {
            List<object> taskList = new List<object>();
            taskList.AddRange(jobInfo.Tasks);
            return taskList;
        }

        public object CreateEmptyTaskObject()
        {
            throw new NotImplementedException();
        }

        public void SetNotifications(string mailAddress, bool? notifyOnStart, bool? notifyOnCompletion, bool? notifyOnFailure)
        {
            throw new NotImplementedException();
        }

        public void SetTasks(List<object> tasks)
        {
            StringBuilder builder = new StringBuilder("");
            foreach (var task in tasks)
            {
                builder.Append((string)task);
            }

            /*jobSource = builder.ToString();*/
        }
        #endregion

        #region Local Methods
        /*protected JobState ConvertJobStateToIndependentJobState(string jobState) {
			/*if (jobState == "H")
				return JobState.Configuring;
			if (jobState == "W")
				return JobState.Submitted;
			if (jobState == "Q" || jobState == "T")
				return JobState.Queued;
			if (jobState == "R" || jobState == "U" || jobState == "S" || jobState == "E" || jobState == "B")
				return JobState.Running;
			if (jobState == "F") {
				if (!string.IsNullOrEmpty(exitStatus)) {
					int exitStatusInt = Convert.ToInt32(exitStatus);
					if (exitStatusInt == 0)
						return JobState.Finished;
					if (exitStatusInt > 0 && exitStatusInt < 256) {
						return JobState.Failed;
					}
					if (exitStatusInt >= 256) {
						return JobState.Canceled;
					}
				}
				return JobState.Canceled;
			}#1#
			throw new ApplicationException("Job state \"" + jobState +
			                               "\" could not be converted to any known job state.");
		}*/

        #endregion
    }
}