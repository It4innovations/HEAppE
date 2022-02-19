using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HEAppE.HpcConnectionFramework.SchedulerAdapters.LinuxLocal.Configuration
{
    internal sealed class CommandScriptPath
    {
        #region LocalHPC Scripts Properties
        private static string LocalHPCScriptDirectory { get; } = "~/.local_hpc_scripts/";

        // <summary>
        /// Path to Prepare LocalHPC job directory
        /// </summary>
        internal static string PrepareJobDirCmdPath { get; } = $"{LocalHPCScriptDirectory}prepare_job_dir.sh";

        /// <summary>
        /// Run local job execution simulation
        /// </summary>
        internal static string RunLocalCmdPath { get; } = $"{LocalHPCScriptDirectory}run_local.sh";

        /// <summary>
        /// Path to execute job info get cmd
        /// </summary>
        internal static string GetJobInfoCmdPath { get; } = $"{LocalHPCScriptDirectory}get_job_info.sh";

        /// <summary>
        /// Path to execute count jobs
        /// </summary>
        internal static string CountJobsCmdPath { get; } = $"{LocalHPCScriptDirectory}count_jobs.sh";

        /// <summary>
        /// Path to execute cancel simulated job
        /// </summary>
        internal static string CancelJobCmdPath { get; } = $"{LocalHPCScriptDirectory}cancel_job.sh";
        #endregion
    }
}
