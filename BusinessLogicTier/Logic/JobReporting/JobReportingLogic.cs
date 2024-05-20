using HEAppE.Exceptions.External;
using HEAppE.BusinessLogicTier.Logic.JobReporting.Converts;
using HEAppE.DataAccessTier.UnitOfWork;
using HEAppE.DomainObjects.ClusterInformation;
using HEAppE.DomainObjects.JobManagement.JobInformation;
using HEAppE.DomainObjects.JobReporting;
using HEAppE.DomainObjects.UserAndLimitationManagement;
using HEAppE.DomainObjects.UserAndLimitationManagement.Enums;
using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HEAppE.DomainObjects.JobManagement;
using Project = HEAppE.DomainObjects.JobManagement.Project;

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
        public IEnumerable<UserGroupListReport> UserGroupListReport(IEnumerable<Project> projects, long userId)
        {

            var adaptorUserGroups = _unitOfWork.AdaptorUserGroupRepository.GetAllWithAdaptorUserGroupsAndActiveProjects().Where(x => projects.Any(y => y.Id == x.ProjectId) && x.AdaptorUserUserGroupRoles.Any(y => y.AdaptorUserId == userId && !y.IsDeleted && y.AdaptorUserRole.ContainedRoleTypes.Any(a => a == AdaptorUserRoleType.GroupReporter)));
            var userGroupReports = adaptorUserGroups.Select(adaptorUserGroup => new UserGroupListReport()
            {
                AdaptorUserGroup = adaptorUserGroup,
                Project = GetProjectReport(adaptorUserGroup.Project, DateTime.MinValue, DateTime.UtcNow, null),
                UsageType = adaptorUserGroup.Project.UsageType
            }).ToList();
            return userGroupReports;
        }

        /// <summary>
        /// Returns resource usage report for Job
        /// </summary>
        /// <param name="jobId">Job ID</param>
        /// <returns></returns>
        /// <exception cref="ApplicationException"></exception>
        public ProjectReport ResourceUsageReportForJob(long jobId, IEnumerable<long> reporterGroupIds)
        {
            var job = _unitOfWork.SubmittedJobInfoRepository.GetById(jobId) ?? throw new ResourceUsageException("JobNotSpecified", jobId);

            if (!reporterGroupIds.Intersect(job.Project.AdaptorUserGroups.Select(x => x.Id)).Any())
            {
                throw new ResourceUsageException("ReporterNoAccessToJob", jobId);
            }

            return new ProjectReport
            {
                Clusters = GetClusterReportsForJob(job),
                Project = job.Project
            };
        }

        /// <summary>
        /// Returns aggregated job reports by state
        /// </summary>
        /// <returns></returns>
        public IEnumerable<JobStateAggregationReport> AggregatedJobsByStateReport(IEnumerable<Project> projects)
        {
            return _unitOfWork.SubmittedJobInfoRepository.GetAll()
                                                            .Where(x => projects.Any(y => y.Id == x.Project.Id))
                                                            .GroupBy(g => g.State)
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
        public IEnumerable<ProjectReport> JobsDetailedReport(IEnumerable<long> groupIds, string[] subProjects)
        {
            return groupIds.Select(groupId => UserGroupResourceUsageReport(groupId, DateTime.MinValue, DateTime.UtcNow, subProjects)).ToList();
        }

        /// <summary>
        /// Returns report for specific user
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="reporterGroupIds"></param>
        /// <param name="startTime">StartTime</param>
        /// <param name="endTime">EndTime</param>
        /// <param name="subProjects"></param>
        /// <returns></returns>
        public IEnumerable<ProjectReport> UserResourceUsageReport(long userId, IEnumerable<long> reporterGroupIds,
            DateTime startTime, DateTime endTime, string[] subProjects)
        {
            AdaptorUser user = _unitOfWork.AdaptorUserRepository.GetById(userId) ?? throw new ResourceUsageException("UserNotSpecified", userId);
            var userGroups = user.Groups.Select(x => x.Id).Distinct().ToList();
            var reporterAndUserGroupsIntersect = reporterGroupIds.Intersect(userGroups);
            return reporterGroupIds.Select(groupId => UserGroupResourceUsageReport(groupId, startTime, endTime, subProjects)).ToList();
        }

        /// <summary>
        /// Returns Report for specific UserGroup
        /// </summary>
        /// <param name="groupId">Group ID</param>
        /// <param name="startTime">StartTime</param>
        /// <param name="endTime">EndTime</param>
        /// <param name="subProjects"></param>
        /// <returns></returns>
        /// <exception cref="ApplicationException"></exception>
        public ProjectReport UserGroupResourceUsageReport(long groupId, DateTime startTime, DateTime endTime,
            string[] subProjects)
        {
            AdaptorUserGroup group = _unitOfWork.AdaptorUserGroupRepository.GetByIdWithAdaptorUserGroups(groupId) ?? throw new ResourceUsageException("GroupNotSpecified", groupId);
            return GetProjectReport(group.Project, startTime, endTime, subProjects);
        }
        
        public ProjectAggregatedReport UserGroupResourceAggregatedUsageReport(long groupId, DateTime startTime, DateTime endTime)
        {
            AdaptorUserGroup group = _unitOfWork.AdaptorUserGroupRepository.GetByIdWithAdaptorUserGroups(groupId) ?? throw new ResourceUsageException("GroupNotSpecified", groupId);
            return GetProjectAggregatedReport(group.Project, startTime, endTime);
        }

        /// <summary>
        /// Returns aggregated UserGroup Resource Usage Report for specific user (all referenced Groups to User)
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="groupIds">Group IDs</param>
        /// <param name="startTime">StartTime</param>
        /// <param name="endTime">EndTime</param>
        /// <returns></returns>
        public IEnumerable<ProjectAggregatedReport> AggregatedUserGroupResourceUsageReport(IEnumerable<long> groupIds, DateTime startTime, DateTime endTime)
        {
            return groupIds.Select(groupId => UserGroupResourceAggregatedUsageReport(groupId, startTime, endTime)).ToList();
        }
        #endregion
        #region Private Methods

        /// <summary>
        /// Returns project report for specified Project
        /// </summary>
        /// <param name="project">Project</param>
        /// <param name="startTime">StartTime</param>
        /// <param name="endTime">EndTime</param>
        /// <param name="subProjects"></param>
        /// <returns></returns>
        private ProjectReport GetProjectReport(Project project, DateTime startTime, DateTime endTime,
            string[] subProjects)
        {
            if (project == null)
            {
                return null;
            }
            var projectReport = new ProjectReport()
            {
                Project = project,
                Clusters = GetClusterReports(project, startTime, endTime, subProjects)
            };
            return projectReport;
        }
        
        private ProjectAggregatedReport GetProjectAggregatedReport(Project project, DateTime startTime, DateTime endTime)
        {
            if (project == null)
            {
                return null;
            }
            var projectReport = new ProjectAggregatedReport()
            {
                Project = project,
                SubProjects = GetSubProjectAggregatedReports(project, startTime, endTime),
                Clusters = GetClusterAggregatedReports(project, startTime, endTime)
            };
            return projectReport;
        }

        private List<SubProjectAggregatedReport> GetSubProjectAggregatedReports(Project project, DateTime startTime, DateTime endTime)
        {
            var subProjectReports = project.SubProjects.Select(subProject => new SubProjectAggregatedReport()
            {
                SubProject = subProject,
                Clusters = GetClusterSubProjectAggregatedReports(subProject, startTime, endTime)
            }).ToList();
            return subProjectReports;
        }

        private List<ClusterAggregatedReport> GetClusterSubProjectAggregatedReports(SubProject subProject, DateTime startTime, DateTime endTime)
        {
            var clusters = subProject.Project.ClusterProjects.Where(cp => cp.Project.Id == subProject.Project.Id)
                .Select(x => x.Cluster)
                .Distinct().ToList();
            var clusterReports = clusters.Select(cluster => new ClusterAggregatedReport()
            {
                Cluster = cluster,
                ClusterNodeTypesAggregations = GetClusterNodeTypeSubProjectAggregationReports(cluster, subProject, startTime, endTime)
            }).ToList();
            return clusterReports;
        }

        private List<ClusterNodeTypeAggregatedReport> GetClusterNodeTypeSubProjectAggregationReports(Cluster cluster, SubProject subProject, DateTime startTime, DateTime endTime)
        {
            var nodeTypeGrouping = cluster.ClusterProjects.SelectMany(x => x.Cluster.NodeTypes)
                .Distinct()
                .OrderBy(x => x.Id)
                .GroupBy(u => u.ClusterNodeTypeAggregation)
                .ToList();

            var reports = new List<ClusterNodeTypeAggregatedReport>();
            foreach (var group in nodeTypeGrouping)
            {
                var aggregationReport = new ClusterNodeTypeAggregatedReport()
                {
                    ClusterNodeTypeAggregation = group.Key,
                    ClusterNodeTypes = group.Select(nodeType => new ClusterNodeTypeReport()
                    {
                        ClusterNodeType = nodeType,
                        Jobs = GetJobSubProjectReports(nodeType.Id, subProject, startTime, endTime)
                    }).ToList()
                };
                reports.Add(aggregationReport);
            }
            return reports;
        }

        private List<JobReport> GetJobSubProjectReports(long nodeTypeId, SubProject subProject, DateTime startTime, DateTime endTime)
        {
            var jobsInSubProject = _unitOfWork.SubmittedJobInfoRepository.GetAllWithSubmittedTaskAdaptorUserAndProject()
                .Where(x => x.Project.Id == subProject.ProjectId &&
                            x.Specification.SubProjectId.HasValue &&
                            x.Specification.SubProjectId.Value == subProject.Id &&
                            x.StartTime >= startTime &&
                            x.EndTime <= endTime &&
                            x.Tasks.Any(y => y.NodeType.Id == nodeTypeId));
            
            var jobReports = jobsInSubProject.Select(job => new JobReport()
            {
                SubmittedJobInfo = job,
                Tasks = GetTaskReportsForJob(job, nodeTypeId, subProject.ProjectId)
            }).ToList();
            return jobReports;
        }

        /// <summary>
        /// Returns Cluster reports for specified Project
        /// </summary>
        /// <param name="project"></param>
        /// <param name="startTime"></param>
        /// <param name="endTime"></param>
        /// <param name="subProjects"></param>
        /// <returns></returns>
        private List<ClusterReport> GetClusterReports(Project project, DateTime startTime, DateTime endTime,
            string[] subProjects)
        {
            var clusters = project.ClusterProjects.Where(cp => cp.Project.Id == project.Id)
                                                    .Select(x => x.Cluster)
                                                        .Distinct().ToList();
            var clusterReports = clusters.Select(cluster => new ClusterReport()
            {
                Cluster = cluster,
                ClusterNodeTypes = GetClusterNodeTypeReports(cluster, project, startTime, endTime, subProjects)
            }).ToList();
            return clusterReports;
        }
        
        private List<ClusterAggregatedReport> GetClusterAggregatedReports(Project project, DateTime startTime, DateTime endTime)
        {
            var clusters = project.ClusterProjects.Where(cp => cp.Project.Id == project.Id)
                .Select(x => x.Cluster)
                .Distinct().ToList();
            var clusterReports = clusters.Select(cluster => new ClusterAggregatedReport()
            {
                Cluster = cluster,
                ClusterNodeTypesAggregations = GetClusterNodeTypeAggregationReports(cluster, project, startTime, endTime)
            }).ToList();
            return clusterReports;
        }

        private List<ClusterNodeTypeAggregatedReport> GetClusterNodeTypeAggregationReports(Cluster cluster, Project project, DateTime startTime, DateTime endTime)
        {
            var nodeTypeGrouping = cluster.ClusterProjects.SelectMany(x => x.Cluster.NodeTypes)
                .Distinct()
                .OrderBy(x => x.Id)
                .GroupBy(u => u.ClusterNodeTypeAggregation)
                .ToList();

            var reports = new List<ClusterNodeTypeAggregatedReport>();
            foreach (var group in nodeTypeGrouping)
            {
                var aggregationReport = new ClusterNodeTypeAggregatedReport()
                {
                    ClusterNodeTypeAggregation = group.Key,
                    ClusterNodeTypes = group.Select(nodeType => new ClusterNodeTypeReport()
                    {
                        ClusterNodeType = nodeType,
                        Jobs = GetJobReports(nodeType.Id, project.Id, startTime, endTime, null)
                    }).ToList()
                };
                reports.Add(aggregationReport);
            }
            return reports;
        }

        /// <summary>
        /// Returns ClusterNodeType reports for specified Project
        /// </summary>
        /// <param name="cluster"></param>
        /// <param name="project">Project</param>
        /// <param name="startTime">StartTime</param>
        /// <param name="endTime">EndTime</param>
        /// <param name="subProjects"></param>
        private List<ClusterNodeTypeReport> GetClusterNodeTypeReports(Cluster cluster, Project project,
            DateTime startTime, DateTime endTime, string[] subProjects)
        {
            var nodeTypes = cluster.ClusterProjects.SelectMany(x => x.Cluster.NodeTypes)
                                                    .Distinct()
                                                        .OrderBy(x => x.Id)
                                                            .ToList();

            var nodeTypeReports = nodeTypes.Select(nodeType => new ClusterNodeTypeReport()
            {
                ClusterNodeType = nodeType,
                Jobs = GetJobReports(nodeType.Id, project.Id, startTime, endTime, subProjects)
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
        /// <param name="subProjects"></param>
        /// <returns></returns>
        private List<JobReport> GetJobReports(long nodeTypeId, long projectId, DateTime startTime, DateTime endTime, string[] subProjects)
        {
            var jobsInProjectQuery = _unitOfWork.SubmittedJobInfoRepository.GetAllWithSubmittedTaskAdaptorUserAndProject()
                .Where(x => x.Project.Id == projectId &&
                            x.StartTime >= startTime &&
                            x.EndTime <= endTime &&
                            x.Tasks.Any(y => y.NodeType.Id == nodeTypeId));
            if (subProjects != null && subProjects.Any())
            {
                jobsInProjectQuery = jobsInProjectQuery.Where(job => subProjects.Contains(job.Specification.SubProject?.Identifier));
            }
            var jobReports = jobsInProjectQuery.Select(job => new JobReport()
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
                x.Project.Id == projectId && x.Specification.ClusterNodeType.Id == nodeTypeId).ToList();
            var taskReports = tasks.Select(task => new TaskReport()
            {
                SubmittedTaskInfo = task,
                Usage = Math.Round(task.ResourceConsumed, 3)
            }).ToList();
            return taskReports;
        }

        /// <summary>
        /// Takes a job and returns a list of reports for each unique cluster in the job
        /// </summary>
        /// <param name="job"></param>
        /// <returns></returns>
        private List<ClusterReport> GetClusterReportsForJob(SubmittedJobInfo job)
        {
            var clusters = job.Tasks.Select(x => x.NodeType.Cluster).Distinct().ToList();
            var clusterReports = clusters.Select(cluster => new ClusterReport()
            {
                Cluster = cluster,
                ClusterNodeTypes = GetClusterNodeTypeReportsForJob(job)
            }).ToList();
            return clusterReports;
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
                    Tasks = new List<TaskReport>()
                };

                foreach (var task in tasksForNodeType.Distinct())
                {
                    var taskReport = new TaskReport()
                    {
                        SubmittedTaskInfo = task,
                        Usage = JobReportingLogicConverts.CalculateUsedResourcesForTask(task) ?? 0
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