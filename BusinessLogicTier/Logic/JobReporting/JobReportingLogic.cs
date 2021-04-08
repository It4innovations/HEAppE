using System.Reflection;
using log4net;
using System.Collections.Generic;
using System.Linq;
using HEAppE.DataAccessTier.UnitOfWork;
using HEAppE.DomainObjects.JobReporting;
using HEAppE.DomainObjects.UserAndLimitationManagement;
using HEAppE.DomainObjects.ClusterInformation;
using HEAppE.BusinessLogicTier.Factory;
using HEAppE.DomainObjects.JobManagement.JobInformation;
using System;
using HEAppE.BusinessLogicTier.Logic.JobReporting.Converts;

namespace HEAppE.BusinessLogicTier.Logic.JobReporting
{
    internal class JobReportingLogic : IJobReportingLogic
    {
        protected static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        protected readonly IUnitOfWork unitOfWork;

        internal JobReportingLogic(IUnitOfWork unitOfWork)
        {
            this.unitOfWork = unitOfWork;
        }

        public IList<AdaptorUserGroup> ListAdaptorUserGroups()
        {
            return unitOfWork.AdaptorUserGroupRepository.GetAll();
        }

        public SubmittedJobInfoUsageReport GetResourceUsageReportForJob(long jobId)
        {
            return unitOfWork.SubmittedJobInfoRepository.GetById(jobId)?.ConvertToUsageReport()?? new SubmittedJobInfoUsageReport();
        }


        public virtual UserResourceUsageReport GetUserResourceUsageReport(long userId, DateTime startTime, DateTime endTime)
        {
            AdaptorUser user = unitOfWork.AdaptorUserRepository.GetById(userId);
            var userTotalUsage = GetResourceUsageForUser(user, startTime, endTime, out ICollection<NodeTypeAggregatedUsage> userNodeTypeAggregatedUsage);

            UserResourceUsageReport userReport = new UserResourceUsageReport
            {
                User = user,
                NodeTypeReport = userNodeTypeAggregatedUsage,
                TotalUsage = userTotalUsage,
                StartTime = startTime,
                EndTime = endTime
            };
            return userReport;
        }

        public virtual UserGroupResourceUsageReport GetUserGroupResourceUsageReport(long userId, long groupId, DateTime startTime, DateTime endTime)
        {
            double? groupTotalUsage = 0;
            var userAggregatedReports = new List<UserAggregatedUsage>();

            var userResourceUsageReport = GetUserResourceUsageReport(userId, startTime, endTime).ConvertUsageReportToAggregatedUsage();
            groupTotalUsage += userResourceUsageReport?.TotalUsage;

            userAggregatedReports.Add(userResourceUsageReport);

            //TODO prepare for all users but must be solved HEAppEUserRoles
            //AdaptorUserGroup group = unitOfWork.AdaptorUserGroupRepository.GetById(groupId);
            //foreach (AdaptorUser user in group.Users)
            //{
            //    var userResourceUsageReport = GetUserResourceUsageReport(user.Id, startTime, endTime).ConvertUsageReportToAggregatedUsage();
            //    groupTotalUsage += userResourceUsageReport?.TotalUsage;

            //    userAggregatedReports.Add(userResourceUsageReport);
            //}

            var userGroupReport = new UserGroupResourceUsageReport
            {
                UserReport = userAggregatedReports,
                TotalUsage = groupTotalUsage,
                StartTime = startTime,
                EndTime = endTime
            };
            return userGroupReport;
        }

        #region Private Methods
        private double? GetResourceUsageForUser(AdaptorUser user, DateTime startTime, DateTime endTime, out ICollection<NodeTypeAggregatedUsage> nodeTypeAggregatedUsage)
        {
            double? userTotalUsage = 0;
            var selectedJobs = LogicFactory.GetLogicFactory().CreateJobManagementLogic(unitOfWork)
                                                             .ListJobsForUser(user).Where(w => w.SubmitTime >= startTime && w.SubmitTime <= endTime)
                                                              .ToList();

            nodeTypeAggregatedUsage = new List<NodeTypeAggregatedUsage>();
            if (selectedJobs is null)
            {
                return default;
            }

            var nodeTypes = LogicFactory.GetLogicFactory().CreateClusterInformationLogic(unitOfWork).ListClusterNodeTypes();
            foreach (ClusterNodeType nodeType in nodeTypes)
            {
                double? nodeTotalUsage = 0;

                //Job with tasks reports
                var tasksInfoUsageReport = new List<SubmittedTaskInfoExtendedUsageReport>();
                foreach (var job in selectedJobs)
                {
                    var selectedTasksInfoUsageReport = job.Tasks.Where(w => w.NodeType == nodeType).Select(s => s.ConvertToExtendedUsageReport(job)).ToList();

                    tasksInfoUsageReport.AddRange(selectedTasksInfoUsageReport);
                    nodeTotalUsage += selectedTasksInfoUsageReport.Sum(s => s.Usage);
                }
                userTotalUsage += nodeTotalUsage;

                //NodeType report
                nodeTypeAggregatedUsage.Add(
                    new NodeTypeAggregatedUsage
                    {
                        NodeType = nodeType,
                        SubmittedTasks = tasksInfoUsageReport,
                        TotalUsage = nodeTotalUsage
                    }
                );
            }
            return userTotalUsage;
        }
        #endregion
    }
}