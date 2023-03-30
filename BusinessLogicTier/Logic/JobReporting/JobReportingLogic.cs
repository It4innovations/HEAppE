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
using HEAppE.DomainObjects.JobManagement;
using HEAppE.DomainObjects.JobManagement.JobInformation;
using HEAppE.OpenStackAPI.DTO.JsonTypes.Authentication;
using Project = HEAppE.DomainObjects.JobManagement.Project;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

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

        /// <summary>
        /// Returns list of all UserGroups and all Projects in groups
        /// </summary>
        /// <returns></returns>
        public IEnumerable<UserGroupListReport> UserGroupListReport()
        {
            var adaptorUserGroups = _unitOfWork.AdaptorUserGroupRepository.GetAllWithAdaptorUserGroupsAndProject();
            var userGroupReports = adaptorUserGroups.Select(adaptorUserGroup => new UserGroupListReport()
            {
                AdaptorUserGroup = adaptorUserGroup,
                Project = GetProjectReport(adaptorUserGroup.Project, DateTime.MinValue, DateTime.UtcNow),
                UsageType = DomainObjects.JobReporting.Enums.UsageType.CoreHours
            }).ToList();
            return userGroupReports;
        }

        /// <summary>
        /// Returns resource usage report for Job
        /// </summary>
        /// <param name="jobId">Job ID</param>
        /// <returns></returns>
        /// <exception cref="ApplicationException"></exception>
        public ProjectReport ResourceUsageReportForJob(long jobId)
        {
            var job = _unitOfWork.SubmittedJobInfoRepository.GetById(jobId);
            if (job is null)
            {
                throw new ApplicationException($"Specified Job Id: \"{jobId}\" is not specified in system!");
            }

            var projectReport = new ProjectReport
            {
                ClusterNodeTypes = GetClusterNodeTypeReportsForJob(job),
                Project = job.Project
            };
            return projectReport;
        }

        /// <summary>
        /// Returns aggregated job reports by state
        /// </summary>
        /// <returns></returns>
        public IEnumerable<JobStateAggregationReport> AggregatedJobsByStateReport()
        {
            return _unitOfWork.SubmittedJobInfoRepository.GetAll().GroupBy(g => g.State)
                                                                  .Select(s => new JobStateAggregationReport
                                                                  {
                                                                      State = s.Key,
                                                                      Count = s.Count()
                                                                  }).ToList();
        }

        /// <summary>
        /// Returns Resource Usage Report for all Jobs
        /// </summary>
        /// <returns></returns>
        public IEnumerable<UserGroupReport> JobsDetailedReport(IEnumerable<long> groupIds)
        {
            return AggregatedUserGroupResourceUsageReport(groupIds, DateTime.MinValue, DateTime.UtcNow);
        }

        /// <summary>
        /// Returns report for specific user
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="startTime">StartTime</param>
        /// <param name="endTime">EndTime</param>
        /// <returns></returns>
        public UserResourceUsageReport UserResourceUsageReport(long userId, DateTime startTime, DateTime endTime)
        {
            AdaptorUser user = _unitOfWork.AdaptorUserRepository.GetById(userId);

            var projectReports = user.AdaptorUserUserGroupRoles.Select(x => x.AdaptorUserGroup.Project)
                                                                .Distinct()
                                                                .Select(x => GetProjectReport(x, startTime, endTime))
                                                                .ToList();

            var userReport = new UserResourceUsageReport
            {
                UsageType = DomainObjects.JobReporting.Enums.UsageType.CoreHours,
                Projects = projectReports,
                TotalUsage = projectReports.Sum(x => x.TotalUsage)
            };
            return userReport;
        }

        /// <summary>
        /// Returns Report for specific UserGroup
        /// </summary>
        /// <param name="groupId">Group ID</param>
        /// <param name="startTime">StartTime</param>
        /// <param name="endTime">EndTime</param>
        /// <returns></returns>
        /// <exception cref="ApplicationException"></exception>
        public UserGroupReport UserGroupResourceUsageReport(long groupId, DateTime startTime, DateTime endTime)
        {
            AdaptorUserGroup group = _unitOfWork.AdaptorUserGroupRepository.GetByIdWithAdaptorUserGroups(groupId);
            if (group is null)
            {
                throw new ApplicationException($"Specified Group Id: \"{groupId}\" is not specified in system!");
            }

            var userGroupReport = new UserGroupReport
            {
                AdaptorUserGroup = group,
                Project = GetProjectReport(group.Project, startTime, endTime),
                UsageType = DomainObjects.JobReporting.Enums.UsageType.CoreHours
            };
            return userGroupReport;
        }

        /// <summary>
        /// Returns aggregated UserGroup Resource Usage Report for specific user (all referenced Groups to User)
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="groupIds">Group IDs</param>
        /// <param name="startTime">StartTime</param>
        /// <param name="endTime">EndTime</param>
        /// <returns></returns>
        public IEnumerable<UserGroupReport> AggregatedUserGroupResourceUsageReport(IEnumerable<long> groupIds, DateTime startTime, DateTime endTime)
        {
            return groupIds.Select(groupId => UserGroupResourceUsageReport(groupId, startTime, endTime)).ToList();
        }
        #endregion
        #region Private Methods
        /// <summary>
        /// Returns project report for specified Project
        /// </summary>
        /// <param name="project">Project</param>
        /// <param name="startTime">StartTime</param>
        /// <param name="endTime">EndTime</param>
        /// <returns></returns>
        private ProjectReport GetProjectReport(Project project, DateTime startTime, DateTime endTime)
        {
            var projectReport = new ProjectReport()
            {
                Project = project,
                ClusterNodeTypes = GetClusterNodeTypeReports(project, startTime, endTime)
            };
            return projectReport;
        }

        /// <summary>
        /// Returns ClusterNodeType reports for specified Project
        /// </summary>
        /// <param name="project">Project</param>
        /// <param name="startTime">StartTime</param>
        /// <param name="endTime">EndTime</param>
        private List<ClusterNodeTypeReport> GetClusterNodeTypeReports(Project project, DateTime startTime, DateTime endTime)
        {
            var nodeTypes = project.CommandTemplates.Select(x => x.ClusterNodeType).Distinct().ToList();

            var nodeTypeReports = nodeTypes.Select(nodeType => new ClusterNodeTypeReport()
            {
                ClusterNodeType = nodeType,
                Jobs = GetJobReports(nodeType.Id, project.Id, startTime, endTime)
            }).ToList();
            return nodeTypeReports;
        }

        /// <summary>
        /// Returns job reports for specified NodeType and Project
        /// </summary>
        /// <param name="nodeTypeId">ClusterNodeType ID</param>
        /// <param name="projectId">Project ID</param>
        /// <param name="startTime">StartTime</param>
        /// <param name="endTime">EndTime</param>
        /// <returns></returns>
        private List<JobReport> GetJobReports(long nodeTypeId, long projectId, DateTime startTime, DateTime endTime)
        {
            var jobsInProject = _unitOfWork.SubmittedJobInfoRepository.GetAllWithSubmittedTaskAdaptorUserAndProject()
                                                                                    .Where(x => x.Project.Id == projectId &&
                                                                                                x.CreationTime >= startTime &&
                                                                                                x.EndTime <= endTime)
                                                                                    .ToList();
            var jobReports = jobsInProject.Select(job => new JobReport()
            {
                SubmittedJobInfo = job,
                Tasks = GetTaskReportsForJob(job, nodeTypeId, projectId)
            }).ToList();
            return jobReports;
        }

        /// <summary>
        /// Returns list of task reports for specified Job
        /// </summary>
        /// <param name="job">Job</param>
        /// <param name="nodeTypeId">ClusterNodeType ID</param>
        /// <param name="projectId">Project ID</param>
        /// <returns></returns>
        private List<TaskReport> GetTaskReportsForJob(SubmittedJobInfo job, long nodeTypeId, long projectId)
        {
            var tasks = job.Tasks.Where(x =>
                x.Project.Id == projectId && x.Specification.ClusterNodeType.Id == nodeTypeId);
            var taskReports = tasks.Select(task => new TaskReport()
            {
                SubmittedTaskInfo = task,
                Usage = JobReportingLogicConverts.CalculateUsedResourcesForTask(task)
            }).ToList();
            return taskReports;
        }

        /// <summary>
        /// Takes a job and returns a list of reports for each unique node type in the job
        /// </summary>
        /// <param name="job">Job</param>
        /// <returns></returns>
        private List<ClusterNodeTypeReport> GetClusterNodeTypeReportsForJob(SubmittedJobInfo job)
        {
            // Get a list of unique node types in the job
            var nodeTypes = job.Tasks
                .Select(x => x.NodeType)
                .Distinct()
                .ToList();

            var jobReports = new List<JobReport>();
            var reports = new List<ClusterNodeTypeReport>();
            foreach (var nodeType in nodeTypes)
            {

                var tasksForNodeType = job.Tasks.Where(x => x.NodeType.Id == nodeType.Id);
                var jobReport = new JobReport()
                {
                    SubmittedJobInfo = job,
                };

                foreach (var task in tasksForNodeType.Distinct())
                {
                    var taskReport = new TaskReport()
                    {
                        SubmittedTaskInfo = task,
                        Usage = JobReportingLogicConverts.CalculateUsedResourcesForTask(task)
                    };
                    jobReport.Tasks.Add(taskReport);

                }
                jobReports.Add(jobReport);
                var nodeTypeReport = new ClusterNodeTypeReport()
                {
                    ClusterNodeType = nodeType,
                    Jobs = jobReports
                };

                reports.Add(nodeTypeReport);
            }
            return reports;
        }
    }
}
#endregion