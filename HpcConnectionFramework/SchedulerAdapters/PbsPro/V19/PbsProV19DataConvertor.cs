using HEAppE.DomainObjects.JobManagement;
using HEAppE.DomainObjects.JobManagement.JobInformation;
using HEAppE.HpcConnectionFramework.SchedulerAdapters.ConversionAdapter;
using HEAppE.HpcConnectionFramework.SchedulerAdapters.PbsPro.Generic;
using HEAppE.MiddlewareUtils;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HEAppE.HpcConnectionFramework.SchedulerAdapters.PbsPro.V19
{
    public class PbsProV19DataConvertor : PbsProDataConvertor
    {
        #region Constructors
        public PbsProV19DataConvertor(ConversionAdapterFactory conversionAdapterFactory) : base(conversionAdapterFactory) { }
        #endregion
        #region SchedulerDataConvertor Members
        //public override SubmittedTaskInfo ConvertTaskToTaskInfo(object task)
        //{
        //    SubmittedTaskInfo taskInfo = new SubmittedTaskInfo();
        //    ISchedulerTaskAdapter taskAdapter = conversionAdapterFactory.CreateTaskAdapter(task);
        //    taskInfo.TaskAllocationNodes = taskAdapter.AllocatedCoreIds?.Select(s => new SubmittedTaskAllocationNodeInfo
        //    {
        //        AllocationNodeId = s,
        //        SubmittedTaskInfoId = long.Parse(taskAdapter.Name)
        //    }).ToList();
        //    taskInfo.ScheduledJobId = taskAdapter.Id;
        //    taskInfo.Priority = taskAdapter.Priority;
        //    taskInfo.Name = taskAdapter.Name;
        //    taskInfo.State = taskAdapter.State;
        //    taskInfo.StartTime = taskAdapter.StartTime;
        //    taskInfo.EndTime = taskAdapter.EndTime;
        //    taskInfo.ErrorMessage = taskAdapter.ErrorMessage;
        //    taskInfo.AllocatedTime = taskAdapter.AllocatedTime;
        //    taskInfo.AllParameters = StringUtils.ConvertDictionaryToString(taskAdapter.AllParameters);
        //    return taskInfo;
        //}
        #endregion
    }
}