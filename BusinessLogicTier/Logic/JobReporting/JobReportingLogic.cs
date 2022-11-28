using HEAppE.BusinessLogicTier.Logic.JobReporting.Converts;
using HEAppE.DataAccessTier.UnitOfWork;
using HEAppE.DomainObjects.ClusterInformation;
using HEAppE.DomainObjects.JobReporting;
using HEAppE.DomainObjects.UserAndLimitationManagement;
using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace HEAppE.BusinessLogicTier.Logic.JobReporting
{
    internal class JobReportingLogic : IJobReportingLogic
    {
        #region Instance
        /// <summary>
        /// Unit of work
        /// </summary>
        protected readonly IUnitOfWork _unitOfWork;

        /// <summary>
        /// Log instance
        /// </summary>
        protected readonly ILog _log;
        #endregion
        #region Constructors
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="unitOfWork">Unit of work</param>
        internal JobReportingLogic(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
            _log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        }
        #endregion
        #region IJobReporting Methods
        public IEnumerable<AdaptorUserGroup> GetAdaptorUserGroups()
        {
            return _unitOfWork.AdaptorUserGroupRepository.GetAllWithAdaptorUserGroups();
        }

        public IEnumerable<JobStateAggregationReport> GetAggregatedJobsByStateReport()
        {
            return _unitOfWork.SubmittedJobInfoRepository.GetAll().GroupBy(g => g.State)
                                                                  .Select(s => new JobStateAggregationReport
                                                                  {
                                                                      State = s.Key,
                                                                      Count = s.Count()
                                                                  }).ToList();
        }

        public IEnumerable<SubmittedJobInfoUsageReport> GetResourceUsageReport()
        {
            return _unitOfWork.SubmittedJobInfoRepository.GetAllWithSubmittedTaskAdaptorUserAndProject().Select(s => s.ConvertToUsageReport());
        }

        public SubmittedJobInfoUsageReport GetResourceUsageReportForJob(long jobId)
        {
            return _unitOfWork.SubmittedJobInfoRepository.GetById(jobId)?.ConvertToUsageReport() ?? new SubmittedJobInfoUsageReport();
        }

        public UserResourceUsageReport GetUserResourceUsageReport(long userId, DateTime startTime, DateTime endTime)
        {
            AdaptorUser user = _unitOfWork.AdaptorUserRepository.GetById(userId);
            var userTotalUsage = GetResourceUsageForUser(user, startTime, endTime, out ICollection<NodeTypeAggregatedUsage> userNodeTypeAggregatedUsage);

            var userReport = new UserResourceUsageReport
            {
                User = user,
                NodeTypeReport = userNodeTypeAggregatedUsage,
                TotalUsage = userTotalUsage,
                StartTime = startTime,
                EndTime = endTime
            };
            return userReport;
        }

        public UserGroupResourceUsageReport GetUserGroupResourceUsageReport(long userId, long groupId, DateTime startTime, DateTime endTime)
        {
            double? groupTotalUsage = 0;
            var userAggregatedReports = new List<UserAggregatedUsage>();

            AdaptorUserGroup group = _unitOfWork.AdaptorUserGroupRepository.GetByIdWithAdaptorUserGroups(groupId);
            if (group is null)
            {
                throw new ApplicationException($"Specified Group Id: \"{groupId}\" is not specified in system!");
            }

            foreach (AdaptorUser user in group.Users)
            {
                var userResourceUsageReport = GetUserResourceUsageReport(user.Id, startTime, endTime).ConvertUsageReportToAggregatedUsage();
                groupTotalUsage += userResourceUsageReport?.TotalUsage;

                userAggregatedReports.Add(userResourceUsageReport);
            }

            var userGroupReport = new UserGroupResourceUsageReport
            {
                UserReport = userAggregatedReports,
                TotalUsage = groupTotalUsage,
                StartTime = startTime,
                EndTime = endTime
            };
            return userGroupReport;
        }
        #endregion
        #region Private Methods
        private double? GetResourceUsageForUser(AdaptorUser user, DateTime startTime, DateTime endTime, out ICollection<NodeTypeAggregatedUsage> nodeTypeAggregatedUsage)
        {
            double? userTotalUsage = 0;
            nodeTypeAggregatedUsage = new List<NodeTypeAggregatedUsage>();

            var selectedJobs = _unitOfWork.SubmittedJobInfoRepository.GetAllForSubmitterId(user.Id).Where(w => w.SubmitTime >= startTime && w.SubmitTime <= endTime);

            if (selectedJobs is null)
            {
                return default;
            }

            var nodeTypes = _unitOfWork.ClusterNodeTypeRepository.GetAll();
            foreach (ClusterNodeType nodeType in nodeTypes)
            {
                double? nodeTotalUsage = 0;

                var tasksInfoUsageReport = new List<SubmittedTaskInfoUsageReportExtended>();
                foreach (var job in selectedJobs)
                {
                    var selectedTasksInfoUsageReport = job.Tasks.Where(w => w.NodeType == nodeType)
                                                                                                  .Select(s => s.ConvertToExtendedUsageReport(job))
                                                                                                  .ToList();

                    tasksInfoUsageReport.AddRange(selectedTasksInfoUsageReport);
                    nodeTotalUsage += selectedTasksInfoUsageReport.Sum(s => s.Usage);
                }
                userTotalUsage += nodeTotalUsage;

                //NodeType report
                nodeTypeAggregatedUsage.Add(new NodeTypeAggregatedUsage
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