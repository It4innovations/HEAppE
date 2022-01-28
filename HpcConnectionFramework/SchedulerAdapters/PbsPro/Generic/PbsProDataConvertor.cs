using System;
using System.Collections.Generic;
using HEAppE.DomainObjects.JobManagement.JobInformation;
using HEAppE.HpcConnectionFramework.SchedulerAdapters.ConversionAdapter;

namespace HEAppE.HpcConnectionFramework.SchedulerAdapters.PbsPro.Generic
{
    public class PbsProDataConvertor : SchedulerDataConvertor
    {
        #region Constructors
        public PbsProDataConvertor(ConversionAdapterFactory conversionAdapterFactory) : base(conversionAdapterFactory) 
        {
        }

        public override SubmittedTaskInfo ConvertTaskToTaskInfo(object responseMessage)
        {
            throw new System.NotImplementedException();
        }

        public override IEnumerable<string> GetJobIds(string responseMessage)
        {
            throw new System.NotImplementedException();
        }

        public override IEnumerable<SubmittedTaskInfo> ReadParametersFromResponse(object response)
        {
            throw new System.NotImplementedException();
        }
        #endregion
        #region SchedulerDataConvertor Members
        //protected override string ConvertJobName(JobSpecification jobSpecification)
        //{
        //    string result = Regex.Replace(jobSpecification.Name, @"\W+", "_");
        //    return result.Substring(0, (result.Length > 15) ? 15 : result.Length);
        //}

        //protected override string ConvertTaskName(string taskName, JobSpecification jobSpecification)
        //{
        //    string result = Regex.Replace(taskName, @"\W+", "_");
        //    return result.Substring(0, (result.Length > 15) ? 15 : result.Length);
        //}

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