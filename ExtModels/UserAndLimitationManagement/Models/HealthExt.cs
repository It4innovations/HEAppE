using System;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace HEAppE.ExtModels.UserAndLimitationManagement.Models;

/// <summary>
/// Health ext
/// </summary>
[DataContract(Name = "HealthExt")]
[Description("Database and vault health status")]
public class HealthExt
{
    [DataMember(Name = "IsHealthy")]
    [Description("IsHealthy")]
    public bool IsHealthy { get; set; }

    [DataMember(Name = "Timestamp")]
    [Description("Timestamp")]
    public DateTime Timestamp { get; set; }

    [DataMember(Name = "Version")]
    [Description("Version")]
    public string Version { get; set; }

    [DataMember(Name = "Component")]
    [Description("Component")]
    public HealthComponent_ Component { get; set; }

    public class HealthComponent_
    {
        [DataMember(Name = "Database")]
        [Description("Database")]
        public Database_ Database { get; set; }

        [DataMember(Name = "Vault")]
        [Description("Vault")]
        public Vault_ Vault { get; set; }

        public class Database_
        {
            [DataMember(Name = "IsHealthy")]
            [Description("IsHealthy")]
            public bool IsHealthy { get; set; }
        }

        public class Vault_
        {
            [DataMember(Name = "IsHealthy")]
            [Description("IsHealthy")]
            public bool IsHealthy { get; set; }

            [DataMember(Name = "Info")]
            [Description("Info")]
            public VaultInfo_ Info { get; set; }

            public class VaultInfo_
            {
                [DataMember(Name = "Vault")]
                [Description("Vault")]
                public bool Initialized { get; set; }

                [DataMember(Name = "Vault")]
                [Description("Vault")]
                public bool @Sealed { get; set; }

                [DataMember(Name = "Vault")]
                [Description("Vault")]
                public bool StandBy { get; set; }

                [DataMember(Name = "Vault")]
                [Description("Vault")]
                public bool PerformanceStandby { get; set; }
            }
        }
    }

    public override string ToString()
    {
        return $"HealthExt(IsHealthy={IsHealthy}; Timestamp={Timestamp}; Version={Version}; ...)";
    }
}