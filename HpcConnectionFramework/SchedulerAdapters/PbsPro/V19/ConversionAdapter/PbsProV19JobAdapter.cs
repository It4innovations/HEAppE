using System;
using System.Collections.Generic;
using System.Text;
using HEAppE.DomainObjects.JobManagement.JobInformation;
using HEAppE.HpcConnectionFramework.SchedulerAdapters.PbsPro.Generic.ConversionAdapter;

namespace HEAppE.HpcConnectionFramework.SchedulerAdapters.PbsPro.V19.ConversionAdapter
{
    public class PbsProV19JobAdapter : PbsProJobAdapter
    {
        #region Constructors
        /// <summary>
        /// Constructor
        /// </summary>
        public PbsProV19JobAdapter()
        {
        
        }
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="schedulerJobInformation">Job information from HPC Scheduler</param>
        public PbsProV19JobAdapter(object schedulerJobInformation) : base(schedulerJobInformation) 
        { 
        
        }
        #endregion
        #region ISchedulerJobAdapter Members
        //public override string Project
        //{
        //    get
        //    {
        //        string result;
        //        if (qstatInfo.TryGetValue(PbsProJobInfoAttributes.PROJECT, out result))
        //            return result;
        //        return string.Empty;
        //    }
        //    set
        //    {
        //        if (!string.IsNullOrEmpty(value))
        //            jobSource += " -P \"" + value + "\"";
        //    }
        //}

        //public override JobState State
        //{
        //    get
        //    {
        //        string result;
        //        if (qstatInfo.TryGetValue(PbsProJobInfoAttributes.JOB_STATE, out result))
        //        {
        //            string exitStatus;
        //            qstatInfo.TryGetValue(PbsProJobInfoAttributes.EXIT_STATUS, out exitStatus);
        //            return ConvertPbsJobStateToIndependentJobState(result, exitStatus);
        //        }
        //        return JobState.Finished;
        //    }
        //}

        //public override DateTime? EndTime
        //{
        //    get
        //    {
        //        if (State == JobState.Canceled || State == JobState.Failed || State == JobState.Finished)
        //        {
        //            string result;
        //            if (qstatInfo.TryGetValue(PbsProJobInfoAttributes.MTIME, out result))
        //                return PbsProConversionUtils.ConvertQstatDateStringToDateTime(result);
        //        }
        //        return null;
        //    }
        //}

        public override void SetTasks(List<object> tasks)
        {
            StringBuilder builder = new StringBuilder("");
            foreach (var task in tasks)
            {
                builder.Append((string)task);
            }

            jobSource = builder.ToString();
        }
        #endregion

        //#region Local Methods
        //protected JobState ConvertPbsJobStateToIndependentJobState(string jobState, string exitStatus)
        //{
        //    if (jobState == "H")
        //        return JobState.Configuring;
        //    if (jobState == "W")
        //        return JobState.Submitted;
        //    if (jobState == "Q" || jobState == "T")
        //        return JobState.Queued;
        //    if (jobState == "R" || jobState == "U" || jobState == "S" || jobState == "E" || jobState == "B")
        //        return JobState.Running;
        //    if (jobState == "F")
        //    {
        //        if (!string.IsNullOrEmpty(exitStatus))
        //        {
        //            int exitStatusInt = Convert.ToInt32(exitStatus);
        //            if (exitStatusInt == 0)
        //                return JobState.Finished;
        //            if (exitStatusInt > 0 && exitStatusInt < 256)
        //            {
        //                return JobState.Failed;
        //            }
        //            if (exitStatusInt >= 256)
        //            {
        //                return JobState.Canceled;
        //            }
        //        }
        //        return JobState.Canceled;
        //    }
        //    throw new ApplicationException("Job state \"" + jobState +
        //                                   "\" could not be converted to any known job state.");
        //}
        //#endregion
    }
}