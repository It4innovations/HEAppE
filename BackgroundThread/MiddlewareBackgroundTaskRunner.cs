using HEAppE.BackgroundThread.Configuration;
using HEAppE.BackgroundThread.Tasks;
using System;
using System.Collections.Generic;

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
            _tasks.Add(new UpdateUnfinishedJobsTask(TimeSpan.FromSeconds(BackGroundThreadConfiguration.GetAllJobsInformationCheck)));
            _tasks.Add(new CloseConnectionToFinishedJobsTask(TimeSpan.FromSeconds(BackGroundThreadConfiguration.CloseConnectionToFinishedJobsCheck)));
            _tasks.Add(new ClusterAccountRotationJobTask(TimeSpan.FromSeconds(BackGroundThreadConfiguration.ClusterAccountRotationJobCheck)));
            _tasks.Add(new RemoveTemporaryFileTransferKeyTask(TimeSpan.FromSeconds(BackGroundThreadConfiguration.FileTransferKeyRemovalCheck)));
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