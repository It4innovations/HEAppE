using System;
using System.Collections.Generic;
using HEAppE.BackgroundThread.Tasks;
using HEAppE.BusinessLogicTier.Configuration;

namespace HEAppE.BackgroundThread {
	public class MiddlewareBackgroundTaskRunner {
		private readonly List<IBackgroundTask> tasks;

		public MiddlewareBackgroundTaskRunner() {
			tasks = new List<IBackgroundTask>();
			tasks.Add(new GetAllJobsInfo(new TimeSpan(0, 0, 30)));
			//tasks.Add(new SynchronizeJobFileContents(new TimeSpan(0, 0, 30)));
            tasks.Add(new CloseConnectionToFinishedJobs(new TimeSpan(0, 0, 30)));
			tasks.Add(new ClusterAccountRotationJob(new TimeSpan(0, 0, 30)));
		}

		public void Start() {
			foreach (var task in tasks) {
				task.StartTimer();
			}
		}

		public void Stop() {
			foreach (var task in tasks) {
				task.StopTimer();
			}
		}
	}
}