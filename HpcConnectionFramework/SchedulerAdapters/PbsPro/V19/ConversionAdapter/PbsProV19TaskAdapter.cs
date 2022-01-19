using System;
using HEAppE.DomainObjects.JobManagement.JobInformation;
using HEAppE.HpcConnectionFramework.SchedulerAdapters.PbsPro.Generic.ConversionAdapter;

namespace HEAppE.HpcConnectionFramework.SchedulerAdapters.PbsPro.V19.ConversionAdapter
{
    public class PbsProV19TaskAdapter : PbsProTaskAdapter
    {
        #region Constructors
        public PbsProV19TaskAdapter(object taskSource) : base(taskSource) { }
        #endregion

        #region ISchedulerTaskAdapter Members
        public override string Id
        {
            get
            {
                string result;
                if (qstatInfo.TryGetValue(PbsProJobInfoAttributes.JOB_ID, out result))
                    return result;

                return "0";
            }
        }

        public override TaskState State
        {
            get
            {
                string result;
                if (qstatInfo.TryGetValue(PbsProJobInfoAttributes.JOB_STATE, out result))
                {
                    string exitStatus;
                    qstatInfo.TryGetValue(PbsProJobInfoAttributes.EXIT_STATUS, out exitStatus);
                    return ConvertPbsTaskStateToIndependentTaskState(result, exitStatus);
                }
                return TaskState.Finished;
            }
        }

        public override DateTime? EndTime
        {
            get
            {
                if (State == TaskState.Canceled || State == TaskState.Failed || State == TaskState.Finished)
                {
                    string result;
                    if (qstatInfo.TryGetValue(PbsProJobInfoAttributes.MTIME, out result))
                        return PbsProConversionUtils.ConvertQstatDateStringToDateTime(result);
                }
                return null;
            }
        }
        #endregion

        #region Local Methods
        protected virtual TaskState ConvertPbsTaskStateToIndependentTaskState(string taskState, string exitStatus)
        {

            //#error Merge: Zeptat se 
            //if (taskState == "W" || taskState == "H")
            //return TaskState.Submitted;
            if (taskState == "W")
                return TaskState.Submitted;
            if (taskState == "Q" || taskState == "T" || taskState == "H")
                return TaskState.Queued;
            if (taskState == "R" || taskState == "U" || taskState == "S" || taskState == "E" || taskState == "B")
                return TaskState.Running;
            if (taskState == "F" || taskState == "X")
            {
                if (!string.IsNullOrEmpty(exitStatus))
                {
                    int exitStatusInt = Convert.ToInt32(exitStatus);
                    if (exitStatusInt == 0)
                        return TaskState.Finished;
                    if (exitStatusInt > 0 && exitStatusInt < 256)
                    {
                        return TaskState.Failed;
                    }
                    if (exitStatusInt >= 256)
                    {
                        return TaskState.Canceled;
                    }
                }
                return TaskState.Canceled;
            }
            throw new ApplicationException("Task state \"" + taskState +
                                           "\" could not be converted to any known task state.");
        }
        #endregion
    }
}