using System;
using System.Threading;
using System.Timers;
using log4net;
using Timer = System.Timers.Timer;

namespace HEAppE.BackgroundThread.Tasks
{
    internal abstract class AbstractTask : IBackgroundTask
    {
        protected readonly ILog _log;
        private readonly Timer _taskTimer;
        private static readonly object _lockObj = new();

        public AbstractTask(TimeSpan interval)
        {
            _log = LogManager.GetLogger(GetType());
            _taskTimer = new Timer(interval.TotalMilliseconds);
            _taskTimer.Elapsed += TaskTimerElapsed;
        }

        public void StartTimer()
        {
            _taskTimer.Start();
        }

        public void StopTimer()
        {
            _taskTimer.Stop();
        }

        private void TaskTimerElapsed(object sender, ElapsedEventArgs e)
        {
            // Run the task in its own thread
            Thread thread = new(delegate ()
            {
                try
                {
                    lock (_lockObj)
                    {
                        RunTask();
                    }
                }
                catch (Exception ex)
                {
                    _log.Error("An error occured during execution of the background task: {0}", ex);
                }
            })
            {
                Name = $"Timer - {GetType()}"
            };
            thread.Start();
        }

        protected abstract void RunTask();
    }
}