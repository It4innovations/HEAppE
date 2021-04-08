using System;
using System.Collections.Generic;
using HEAppE.BackgroundThread.Tasks;

namespace HEAppE.BackgroundThread {
	public class MiddlewareBackgroundTaskRunner {
		private readonly List<IBackgroundTask> tasks;

		public MiddlewareBackgroundTaskRunner() {
			this.tasks = new List<IBackgroundTask>();
			this.tasks.Add(new GetAllJobsInfo(new TimeSpan(0, 0, 30)));
			//this.tasks.Add(new SynchronizeJobFileContents(new TimeSpan(0, 0, 30)));
            this.tasks.Add(new CloseConnectionToFinishedJobs(new TimeSpan(0, 0, 30)));
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