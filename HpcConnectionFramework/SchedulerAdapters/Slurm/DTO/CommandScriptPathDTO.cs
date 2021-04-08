using System;
using System.Collections.Generic;
using System.Text;

namespace HEAppE.HpcConnectionFramework.SchedulerAdapters.Slurm.DTO
{
    /// <summary>
    /// Class: Clusters scripts used in schedulers
    /// </summary>
    internal sealed class CommandScriptPathDTO
    {
        #region Properties
        /// <summary>
        /// Path to copy data from temp script
        /// </summary>
        internal string CopyDataFromTempCmdPath { get; } = "~/.key_script/copy_data_from_temp.sh"; 

        /// <summary>
        /// Path to copy data to temp script
        /// </summary>
        internal string CopyDataToTempCmdPath { get; } = "~/.key_script/copy_data_to_temp.sh";

        /// <summary>
        /// Path to adding file transfer key script
        /// </summary>
        internal string AddFiletransferKeyCmdPath { get; } = "~/.key_script/add_key.sh";

        /// <summary>
        /// Path to remove file transfer key script
        /// </summary>
        internal string RemoveFiletransferKeyCmdPath { get; } = "~/.key_script/remove_key.sh";

        /// <summary>
        /// Path to create job directory script
        /// </summary>
        internal string CreateJobDirectoryCmdPath { get; } = "~/.key_script/create_job_directory.sh";
        #endregion
    }
}
