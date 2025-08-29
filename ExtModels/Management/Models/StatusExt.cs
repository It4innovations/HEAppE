using HEAppE.ExtModels.JobManagement.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace HEAppE.ExtModels.Management.Models;

/// <summary>
/// Instance status information ext
/// </summary>
[DataContract(Name = "StatusExt")]
[Description("Instance status information ext")]
public class StatusExt
{
    #region Properties
    /// <summary>
    /// ProjectId
    /// </summary>
    [DataMember(Name = "ProjectId")]
    [Description("ProjectId")]
    public int ProjectId { get; set; }

    /// <summary>
    /// TimeFrom
    /// </summary>
    [DataMember(Name = "TimeFrom")]
    [Description("TimeFrom")]
    public DateTime? TimeFrom { get; set; }

    /// <summary>
    /// TimeFrom
    /// </summary>
    [DataMember(Name = "TimeTo")]
    [Description("TimeTo")]
    public DateTime? TimeTo { get; set; }

    /// <summary>
    /// TimeFrom
    /// </summary>
    [DataMember(Name = "Statistics")]
    [Description("Statistics")]
    public StatisticsExt_ Statistics { get; set; }

    /// <summary>
    /// Details
    /// </summary>
    [DataMember(Name = "Details")]
    [Description("Details")]
    public IEnumerable<object> Details { get; set; }

    #endregion

    #region Override methods

    /// <summary>
    ///     Override to string method
    /// </summary>
    /// <returns></returns>
    public override string ToString()
    {
        return $"StatusExt: ProjectId={ProjectId}, TimeFrom={TimeFrom}, TimeTo={TimeTo}, ...";
    }

    #endregion

    #region Internal classes

    public class StatisticsExt_
    {

        #region Properties
        /// <summary>
        /// TotalChecks
        /// </summary>
        [DataMember(Name = "TotalChecks")]
        [Description("TotalChecks")]
        public int TotalChecks { get; set; }

        /// <summary>
        /// VaultCredentialOkCount
        /// </summary>
        [DataMember(Name = "VaultCredentialOkCount")]
        [Description("VaultCredentialOkCount")]
        public int VaultCredentialOkCount { get; set; }

        /// <summary>
        /// VaultCredentialFailCount
        /// </summary>
        [DataMember(Name = "VaultCredentialFailCount")]
        [Description("VaultCredentialFailCount")]
        public int VaultCredentialFailCount { get; set; }

        /// <summary>
        /// ClusterConnectionOkCount
        /// </summary>
        [DataMember(Name = "ClusterConnectionOkCount")]
        [Description("ClusterConnectionOkCount")]
        public int ClusterConnectionOkCount { get; set; }

        /// <summary>
        /// ClusterConnectionFailCount
        /// </summary>
        [DataMember(Name = "ClusterConnectionFailCount")]
        [Description("ClusterConnectionFailCount")]
        public int ClusterConnectionFailCount { get; set; }

        /// <summary>
        /// DryRunJobOkCount
        /// </summary>
        [DataMember(Name = "DryRunJobOkCount")]
        [Description("DryRunJobOkCount")]
        public int DryRunJobOkCount { get; set; }

        /// <summary>
        /// DryRunJobFailCount
        /// </summary>
        [DataMember(Name = "DryRunJobFailCount")]
        [Description("DryRunJobFailCount")]
        public int DryRunJobFailCount { get; set; }

        #endregion

        #region Override methods

        /// <summary>
        ///     Override to string method
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"StatusExt: TotalChecks={TotalChecks}, VaultCredentialOkCount={VaultCredentialOkCount}, VaultCredentialFailCount={VaultCredentialFailCount},ClusterConnectionOkCount={ClusterConnectionOkCount}, ClusterConnectionFailCount={ClusterConnectionFailCount}, DryRunJobOkCount={DryRunJobOkCount}, DryRunJobFailCount={DryRunJobFailCount}";
        }

        #endregion

    }

    #endregion
}