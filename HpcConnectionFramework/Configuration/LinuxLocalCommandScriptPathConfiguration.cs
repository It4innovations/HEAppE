namespace HEAppE.HpcConnectionFramework.Configuration
{
    public sealed class LinuxLocalCommandScriptPathConfiguration
    {
        #region Properties
        // <summary>
        /// Path to Prepare LocalHPC job directory
        /// </summary>
        public string PrepareJobDirCmdPath { get; set; }

        /// <summary>
        /// Run local job execution simulation
        /// </summary>
        public string RunLocalCmdPath { get; set; }

        /// <summary>
        /// Path to execute job info get cmd
        /// </summary>
        public string GetJobInfoCmdPath { get; set; }

        /// <summary>
        /// Path to execute count jobs
        /// </summary>
        public string CountJobsCmdPath { get; set; }

        /// <summary>
        /// Path to execute cancel simulated job
        /// </summary>
        public string CancelJobCmdPath { get; set; }
        #endregion
    }
}
