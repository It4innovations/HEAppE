namespace HEAppE.HpcConnectionFramework.SchedulerAdapters.LinuxLocal.Enums
{
    internal enum LinuxLocalTaskState
    {
        /// <summary>
        /// H: Configuring
        /// </summary>
        H,

        /// <summary>
        /// Q: Queued
        /// </summary>
        Q,

        /// <summary>
        /// O: Failed
        /// </summary>
        O,

        /// <summary>
        /// R: Running
        /// </summary>
        R,

        /// <summary>
        /// F: Finished
        /// </summary>
        F,

        /// <summary>
        /// S: Canceled
        /// </summary>
        S
    }
}
