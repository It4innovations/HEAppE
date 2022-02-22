namespace HEAppE.BackgroundThread.Tasks
{
	internal interface IBackgroundTask
	{
		void StartTimer();
		void StopTimer();
	}
}