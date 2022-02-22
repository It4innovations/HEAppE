using System;
using System.Collections.Generic;
using HEAppE.BackgroundThread.Configuration;
using HEAppE.BackgroundThread.Tasks;
using HEAppE.BusinessLogicTier.Configuration;

namespace HEAppE.BackgroundThread
{
    public class MiddlewareBackgroundTaskRunner
    {
        #region Instances
        private readonly List<IBackgroundTask> _tasks = new();
        #endregion
        #region Constructors
        public MiddlewareBackgroundTaskRunner()
        {
            _tasks.Add(new GetAllJobsInfo(TimeSpan.FromSeconds(BackGroundThreadConfiguration.GetAllJobsInformationCheck)));
            _tasks.Add(new CloseConnectionToFinishedJobs(TimeSpan.FromSeconds(BackGroundThreadConfiguration.CloseConnectionToFinishedJobsCheck)));
            _tasks.Add(new ClusterAccountRotationJob(TimeSpan.FromSeconds(BackGroundThreadConfiguration.ClusterAccountRotationJobCheck)));
        }
        #endregion
        #region Methods
        public void Start()
        {
            foreach (var task in _tasks)
            {
                task.StartTimer();
            }
        }

        public void Stop()
        {
            foreach (var task in _tasks)
            {
                task.StopTimer();
            }
        }
        #endregion
    }
}