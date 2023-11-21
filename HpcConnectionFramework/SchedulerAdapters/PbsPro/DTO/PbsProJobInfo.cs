using HEAppE.Exceptions.Internal;
using HEAppE.DomainObjects.JobManagement.JobInformation;
using HEAppE.HpcConnectionFramework.Configuration;
using HEAppE.HpcConnectionFramework.SchedulerAdapters.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace HEAppE.HpcConnectionFramework.SchedulerAdapters.PbsPro.DTO
{
    /// <summary>
    /// PBS Professional scheduler job information object
    /// </summary>
    public class PbsProJobInfo : ISchedulerJobInfo
    {
        #region Instances
        /// <summary>
        /// Job allocated nodes
        /// </summary>
        private IEnumerable<string> _allocatedNodes = Enumerable.Empty<string>();

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
                    AggregateSchedulerResponseParameters = $"{SchedulerResponseParameters}";
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
        public long Priority { get; set; }

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
        /// Job state name
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
                    _ => throw new PbsException("TaskStateConvertException", value),
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
                        _ => TaskState.Failed,
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
        [Scheduler("ctime", Format = "ddd MMM d HH:mm:ss yyyy")]
        public DateTime CreationTime { get; set; }

        /// <summary>
        /// Job submittion time
        /// </summary>
        [Scheduler("etime", Format = "ddd MMM d HH:mm:ss yyyy")]
        public DateTime SubmitTime { get; set; }

        /// <summary>
        /// Job start time
        /// </summary>
        [Scheduler("stime", Format = "ddd MMM d HH:mm:ss yyyy")]
        public DateTime? StartTime { get; set; }

        /// <summary>
        /// Job end time
        /// </summary>
        [Scheduler("mtime", Format = "ddd MMM d HH:mm:ss yyyy")]
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
        /// Job run number of cores
        /// </summary>
        [Scheduler("resources_used.ncpus")]
        public int? UsedCores { get; set; }

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
                    _allocatedNodes = Regex.Matches(value, @"(?<AllocationNode>[[a-z]+\d*)", RegexOptions.Compiled)
                                                .Where(w => w.Success && !string.IsNullOrEmpty(w.Groups.GetValueOrDefault("AllocationNode").Value))
                                                .Select(s => s.Groups.GetValueOrDefault("AllocationNode").Value)
                                                .ToList();
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
        #region Job Arrays Properties
        /// <summary>
        /// Is job with job arrays 
        /// </summary>
        [Scheduler("array")]
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
        #region Constructors
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="schedulerResponseParameters"></param>
        public PbsProJobInfo(string schedulerResponseParameters)
        {
            SchedulerResponseParameters = schedulerResponseParameters;
        }
        #endregion
        #region Methods
        /// <summary>
        /// Combine two jobs with job arrays parameter
        /// </summary>
        /// <param name="jobInfo">Job info</param>
        public void CombineJobs(PbsProJobInfo jobInfo)
        {
            StartTime = (StartTime > jobInfo.StartTime && jobInfo.StartTime.HasValue) ? StartTime : jobInfo.StartTime;

            if (UsedCores.HasValue && jobInfo.UsedCores.HasValue && UsedCores != jobInfo.UsedCores)
            {
                var normalizedRunTime = (jobInfo.UsedCores.Value * jobInfo.RunTime.TotalSeconds) / UsedCores.Value;
                RunTime += TimeSpan.FromSeconds(Math.Round(normalizedRunTime, 3));
            }
            else
            {
                UsedCores = jobInfo.UsedCores;
                RunTime += jobInfo.RunTime;
            }


            _allocatedNodes = jobInfo.AllocatedNodes.Union(_allocatedNodes);
            AggregateSchedulerResponseParameters += $"\n{HPCConnectionFrameworkConfiguration.JobArrayDbDelimiter}\n{jobInfo.SchedulerResponseParameters}";
        }
        #endregion
    }
}
