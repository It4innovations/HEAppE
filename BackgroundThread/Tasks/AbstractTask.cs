using System;
using System.Threading;
using System.Timers;
using log4net;
using Timer = System.Timers.Timer;

namespace HEAppE.BackgroundThread.Tasks {
	internal abstract class AbstractTask : IBackgroundTask {
		protected readonly ILog log;
		private readonly Timer taskTimer;
        private static readonly Object lockObj = new Object();

        public AbstractTask(TimeSpan interval) {
			this.log = LogManager.GetLogger(this.GetType());
			this.taskTimer = new Timer(interval.TotalMilliseconds);
			this.taskTimer.Elapsed += taskTimer_Elapsed;
		}

		public void StartTimer() {
			this.taskTimer.Start();
		}

		public void StopTimer() {
			this.taskTimer.Stop();
		}

		private void taskTimer_Elapsed(object sender, ElapsedEventArgs e) {
			// Run the task in its own thread
			Thread thread = new Thread(delegate() {
				try {
                    lock (lockObj) {
                        RunTask();
                    }
				}
				catch (Exception ex) {
					log.Error("An error occured during execution of the background task: {0}", ex);
				}
			});
			thread.Name = "Timer - " + this.GetType().ToString();
			thread.Start();
		}

		protected abstract void RunTask();
	}
}