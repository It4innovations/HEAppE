using HEAppE.DomainObjects.JobManagement.JobInformation;
using HEAppE.HpcConnectionFramework.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace HEAppE.HpcConnectionFramework.SchedulerAdapters.PbsPro.DTO
{
    public class PbsProJobInfo : ISchedulerJobInfo
    {
        #region Instances
        /// <summary>
        /// Job allocated nodes
        /// </summary>
        private IEnumerable<string> _allocatedNodes;

        /// <summary>
        /// Job scheduler id
        /// </summary>
        private string _schedulerJobId;

        /// <summary>
        /// Script code exit status
        /// </summary>
        private int? _exitStatus;
        #endregion
        #region Properties
        /// <summary>
        /// Job scheduled id
        /// </summary>
        [Scheduler("Job Id")]
        public string SchedulerJobId
        {
            get
            {
                return _schedulerJobId;
            }
            set
            {
                _schedulerJobId = value;

                var match = Regex.Match(_schedulerJobId, @"(?<IndexArray>(\[[0-9]*\])+)", RegexOptions.Compiled);
                if (match.Success)
                {
                    IsJobArrayJob = true;
                    SchedulerJobIdWoJobArrayIndex = _schedulerJobId.Replace(match.Groups.GetValueOrDefault("IndexArray").Value, string.Empty);
                    AggregateSchedulerResponseParameters = $"{HPCConnectionFrameworkConfiguration.JobArrayDbDelimiter}\n{SchedulerResponseParameters}\n";
                }
            }
        }

        /// <summary>
        /// Job Name
        /// </summary>
        [Scheduler("Job_Name")]
        public string Name { get; set; }

        /// <summary>
        /// Job priority
        /// </summary>
        [Scheduler("Priority")]
        public int Priority { get; set; }

        /// <summary>
        /// Job requeue
        /// </summary>
        [Scheduler("Rerunable")]
        public bool Requeue { get; set; }

        /// <summary>
        /// Job queue name
        /// </summary>
        [Scheduler("queue")]
        public string QueueName { get; set; }

        /// <summary>
        /// Task state name
        /// </summary>
        [Scheduler("job_state")]
        public string StateName
        {
            set
            {
                TaskState = value switch
                {
                    "W" => TaskState.Submitted,
                    "Q" or "T" or "H" => TaskState.Queued,
                    "R" or "U" or "S" or "E" or "B" => TaskState.Running,
                    "F" or "X" => TaskState.Failed,
                    _ => throw new ApplicationException(@$"Task state ""{value}"" could not be converted to any known task state."),
                };
            }
        }

        /// <summary>
        /// Job task state
        /// </summary>
        public TaskState TaskState { get; private set; }

        /// <summary>
        /// Job exit status
        /// </summary>
        [Scheduler("Exit_status")]
        public int? ExitStatus
        {
            set
            {
                _exitStatus = value;
                if (TaskState == TaskState.Failed)
                {
                    TaskState = _exitStatus switch
                    {
                        0 => TaskState.Finished,
                        > 0 and < 256 => TaskState.Canceled,
                        >= 256 => TaskState.Canceled,
                        _ => TaskState.Canceled,
                    };
                }
            }
            get
            {
                return _exitStatus;
            }
        }

        /// <summary>
        /// Job creation time
        /// </summary>
        [Scheduler("ctime")]
        public DateTime CreationTime { get; set; }

        /// <summary>
        /// Job submittion time
        /// </summary>
        [Scheduler("etime")]
        public DateTime SubmitTime { get; set; }

        /// <summary>
        /// Job start time
        /// </summary>
        [Scheduler("stime")]
        public DateTime? StartTime { get; set; }

        /// <summary>
        /// Job end time
        /// </summary>
        [Scheduler("mtime")]
        public DateTime? EndTime { get; set; }

        /// <summary>
        /// Job allocated time (requirement)
        /// </summary>
        [Scheduler("Resource_List.walltime")]
        public TimeSpan AllocatedTime { get; set; }

        /// <summary>
        /// Job run time
        /// </summary>
        [Scheduler("resources_used.walltime")]
        public TimeSpan RunTime { get; set; }

        /// <summary>
        /// Job allocated nodes
        /// </summary>
        [Scheduler("exec_host")]
        public string AllocatedNodesSplit
        {
            set
            {
                if (!string.IsNullOrEmpty(value))
                {
                    _allocatedNodes = Regex.Split(value, @"[[a-z]*\d*]*[\/]\d+").ToList();
                }
                else
                {
                    _allocatedNodes = Enumerable.Empty<string>();
                }
            }
        }

        public IEnumerable<string> AllocatedNodes
        {
            get
            {
                return _allocatedNodes;
            }
        }

        /// <summary>
        /// Job scheduler response raw data
        /// </summary>
        public string SchedulerResponseParameters { get; private set; }
        #endregion
        #region Constructors
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="schedulerResponseParameters"></param>
        public PbsProJobInfo(string schedulerResponseParameters)
        {
            SchedulerResponseParameters = schedulerResponseParameters;
        }
        #region Job Arrays Properties
        /// <summary>
        /// Is job with job arrays 
        /// </summary>
        public bool IsJobArrayJob { get; private set; } = false;

        /// <summary>
        /// Job scheduler id without jobarray index
        /// </summary>
        public string SchedulerJobIdWoJobArrayIndex { get; private set; }

        /// <summary>
        /// Aggregate job scheduler raw response for data
        /// </summary>
        public string AggregateSchedulerResponseParameters { get; private set; }
        #endregion
        #endregion
        #region Methods
        /// <summary>
        /// Combine two jobs with job arrays parameter
        /// </summary>
        /// <param name="jobInfo">Job info</param>
        internal void CombineJobs(PbsProJobInfo jobInfo)
        {
            StartTime = (StartTime > jobInfo.StartTime && jobInfo.StartTime.HasValue) ? StartTime : jobInfo.StartTime;
            EndTime = (EndTime > jobInfo.EndTime && jobInfo.EndTime.HasValue) ? EndTime : jobInfo.EndTime;
            RunTime += jobInfo.RunTime;

            _allocatedNodes = jobInfo.AllocatedNodes.Union(_allocatedNodes);
            AggregateSchedulerResponseParameters += $"{HPCConnectionFrameworkConfiguration.JobArrayDbDelimiter}\n{jobInfo.SchedulerResponseParameters}";
        }
        #endregion
    }
}
