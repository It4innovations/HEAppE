using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace HEAppE.DomainObjects.Management;

/// <summary>
/// Instance status information
/// </summary>
[DataContract(Name = "Status")]
[Description("Instance status information")]
public class Status
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
    public Statistics_ Statistics { get; set; }

    /// <summary>
    /// Details
    /// </summary>
    [DataMember(Name = "Details")]
    [Description("Details")]
    public IEnumerable<Detail_> Details { get; set; }

    #endregion

    #region Override methods

    /// <summary>
    ///     Override to string method
    /// </summary>
    /// <returns></returns>
    public override string ToString()
    {
        return $"Status: ProjectId={ProjectId}, TimeFrom={TimeFrom}, TimeTo={TimeTo}, ...";
    }

    #endregion

    #region Internal classes

    public class Statistics_
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
        public VaultCredentialCounts_ VaultCredential { get; set; }

        /// <summary>
        /// ClusterConnection
        /// </summary>
        [DataMember(Name = "ClusterConnection")]
        [Description("ClusterConnection")]
        public ClusterConnectionCounts_ ClusterConnection { get; set; }

        /// <summary>
        /// DryRunJob
        /// </summary>
        [DataMember(Name = "DryRunJob")]
        [Description("DryRunJob")]
        public ClusterConnectionCounts_ DryRunJob { get; set; }

        #endregion

        #region Override methods

        /// <summary>
        ///     Override to string method
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"Statistics_: TotalChecks={TotalChecks}, VaultCredential={VaultCredential}, ClusterConnection={ClusterConnection},DryRunJob={DryRunJob}";
        }

        #endregion

    }

    public abstract class OkFailCounts_
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
            return $"Status: OkCount={OkCount}, FailCount={FailCount}";
        }
    }
    public class VaultCredentialCounts_ : OkFailCounts_;
    public class ClusterConnectionCounts_ : OkFailCounts_;
    public class DryRunJobCounts_ : OkFailCounts_;

    public class Detail_
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
        public ClusterAuthenticationCredential_ ClusterAuthenticationCredential { get; set; }

        /// <summary>
        /// VaultCredential
        /// </summary>
        [DataMember(Name = "VaultCredential")]
        [Description("VaultCredential")]
        public VaultCredentialCounts_ VaultCredential { get; set; }

        /// <summary>
        /// ClusterConnection
        /// </summary>
        [DataMember(Name = "ClusterConnection")]
        [Description("ClusterConnection")]
        public ClusterConnectionCounts_ ClusterConnection { get; set; }

        /// <summary>
        /// ClusterConnection
        /// </summary>
        [DataMember(Name = "DryRunJob")]
        [Description("DryRunJob")]
        public DryRunJobCounts_ DryRunJob { get; set; }

        public class ClusterAuthenticationCredential_
        {
            /// <summary>
            /// Id
            /// </summary>
            [DataMember(Name = "Id")]
            [Description("Id")]
            public int Id { get; set; }

            /// <summary>
            /// Username
            /// </summary>
            [DataMember(Name = "Username")]
            [Description("Username")]
            public string Username { get; set; }

        }
    }

    #endregion
}