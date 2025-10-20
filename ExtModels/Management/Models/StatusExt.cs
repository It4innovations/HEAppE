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
    public long ProjectId { get; set; }

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
    public IEnumerable<DetailExt_> Details { get; set; }

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
        /// VaultCredential
        /// </summary>
        [DataMember(Name = "VaultCredential")]
        [Description("VaultCredential")]
        public VaultCredentialCountsExt_ VaultCredential { get; set; }

        /// <summary>
        /// ClusterConnection
        /// </summary>
        [DataMember(Name = "ClusterConnection")]
        [Description("ClusterConnection")]
        public ClusterConnectionCountsExt_ ClusterConnection { get; set; }

        /// <summary>
        /// DryRunJob
        /// </summary>
        [DataMember(Name = "DryRunJob")]
        [Description("DryRunJob")]
        public ClusterConnectionCountsExt_ DryRunJob { get; set; }

        #endregion

        #region Override methods

        /// <summary>
        ///     Override to string method
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"StatisticsExt_: TotalChecks={TotalChecks}, VaultCredential={VaultCredential}, ClusterConnection={ClusterConnection},DryRunJob={DryRunJob}";
        }

        #endregion

    }

    public abstract class OkFailCountsExt_
    {
        /// <summary>
        /// OkCount
        /// </summary>
        [DataMember(Name = "OkCount")]
        [Description("OkCount")]
        public int OkCount { get; set; }

        /// <summary>
        /// FailCount
        /// </summary>
        [DataMember(Name = "FailCount")]
        [Description("FailCount")]
        public int FailCount { get; set; }

        /// <summary>
        ///     Override to string method
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"StatusExt: OkCount={OkCount}, FailCount={FailCount}";
        }
    }
    public class VaultCredentialCountsExt_ : OkFailCountsExt_;
    public class ClusterConnectionCountsExt_ : OkFailCountsExt_;
    public class DryRunJobCountsExt_ : OkFailCountsExt_;

    public class DetailExt_
    {
        /// <summary>
        /// CheckTimestamp
        /// </summary>
        [DataMember(Name = "CheckTimestamp")]
        [Description("CheckTimestamp")]
        public DateTime CheckTimestamp { get; set; }

        /// <summary>
        /// ClusterAuthenticationCredential
        /// </summary>
        [DataMember(Name = "ClusterAuthenticationCredential")]
        [Description("ClusterAuthenticationCredential")]
        public ClusterAuthenticationCredentialExt_ ClusterAuthenticationCredential { get; set; }

        /// <summary>
        /// VaultCredential
        /// </summary>
        [DataMember(Name = "VaultCredential")]
        [Description("VaultCredential")]
        public VaultCredentialCountsExt_ VaultCredential { get; set; }

        /// <summary>
        /// ClusterConnection
        /// </summary>
        [DataMember(Name = "ClusterConnection")]
        [Description("ClusterConnection")]
        public ClusterConnectionCountsExt_ ClusterConnection { get; set; }

        /// <summary>
        /// DryRunJob
        /// </summary>
        [DataMember(Name = "DryRunJob")]
        [Description("DryRunJob")]
        public DryRunJobCountsExt_ DryRunJob { get; set; }

        /// <summary>
        /// Errors
        /// </summary>
        [DataMember(Name = "Errors")]
        [Description("Errors")]
        public List<CheckLogExt_> Errors { get; set; } = null;

        public class ClusterAuthenticationCredentialExt_
        {
            /// <summary>
            /// Id
            /// </summary>
            [DataMember(Name = "Id")]
            [Description("Id")]
            public long Id { get; set; }

            /// <summary>
            /// Username
            /// </summary>
            [DataMember(Name = "Username")]
            [Description("Username")]
            public string Username { get; set; }

        }

        public class CheckLogExt_
        {
            /// <summary>
            /// CheckTimestamp
            /// </summary>
            [DataMember(Name = "CheckTimestamp")]
            [Description("CheckTimestamp")]
            public DateTime CheckTimestamp { get; set; }

            /// <summary>
            /// VaultCredentialOk
            /// </summary>
            [DataMember(Name = "VaultCredentialOk")]
            [Description("VaultCredentialOk")]
            public bool? VaultCredentialOk { get; set; }

            /// <summary>
            /// ClusterConnectionOk
            /// </summary>
            [DataMember(Name = "ClusterConnectionOk")]
            [Description("ClusterConnectionOk")]
            public bool? ClusterConnectionOk { get; set; }

            /// <summary>
            /// DryRunJobOk
            /// </summary>
            [DataMember(Name = "DryRunJobOk")]
            [Description("DryRunJobOk")]
            public bool? DryRunJobOk { get; set; }

            /// <summary>
            /// ErrorMessage
            /// </summary>
            [DataMember(Name = "ErrorMessage")]
            [Description("ErrorMessage")]
            public string ErrorMessage { get; set; }
        }
    }

    #endregion
}

public class StatusCheckLogsExt
{
    /// <summary>
    /// ByClusterAuthenticationCredential
    /// </summary>
    [DataMember(Name = "ByClusterAuthenticationCredential")]
    [Description("ByClusterAuthenticationCredential")]
    public IEnumerable<ByClusterAuthenticationCredentialExt> ByClusterAuthenticationCredential { get; set; }

    public class ByClusterAuthenticationCredentialExt
    {
        /// <summary>
        /// ClusterAuthenticationCredential
        /// </summary>
        [DataMember(Name = "ClusterAuthenticationCredential")]
        [Description("ClusterAuthenticationCredential")]
        public ClusterAuthenticationCredentialExt ClusterAuthenticationCredential { get; set; }

        /// <summary>
        /// CheckLogs
        /// </summary>
        [DataMember(Name = "CheckLogs")]
        [Description("CheckLogs")]
        public IEnumerable<CheckLogExt> CheckLogs { get; set; }

        public class ClusterAuthenticationCredentialExt
        {
            /// <summary>
            /// Id
            /// </summary>
            [DataMember(Name = "Id")]
            [Description("Id")]
            public long Id { get; set; }

            /// <summary>
            /// Username
            /// </summary>
            [DataMember(Name = "Username")]
            [Description("Username")]
            public string Username { get; set; }
        }

        public class CheckLogExt
        {
            /// <summary>
            /// CheckTimestamp
            /// </summary>
            [DataMember(Name = "CheckTimestamp")]
            [Description("CheckTimestamp")]
            public DateTime CheckTimestamp { get; set; }

            /// <summary>
            /// VaultCredentialOk
            /// </summary>
            [DataMember(Name = "VaultCredentialOk")]
            [Description("VaultCredentialOk")]
            public bool? VaultCredentialOk { get; set; }

            /// <summary>
            /// ClusterConnectionOk
            /// </summary>
            [DataMember(Name = "ClusterConnectionOk")]
            [Description("ClusterConnectionOk")]
            public bool? ClusterConnectionOk { get; set; }

            /// <summary>
            /// DryRunJobOk
            /// </summary>
            [DataMember(Name = "DryRunJobOk")]
            [Description("DryRunJobOk")]
            public bool? DryRunJobOk { get; set; }

            /// <summary>
            /// ErrorMessage
            /// </summary>
            [DataMember(Name = "ErrorMessage")]
            [Description("ErrorMessage")]
            public string ErrorMessage { get; set; }
        }
    }
}