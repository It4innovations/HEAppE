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
    
    [DataMember(Name = "InstanceIdentifier")]
    [Description("Instance Identifier")]
    public string InstanceIdentifier { get; set; }

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
            [Description("Database is healthy")]
            public bool IsHealthy { get; set; }

            public override string ToString()
            {
                return $"Database_(IsHealthy={IsHealthy})";
            }
        }

        public class Vault_
        {
            [DataMember(Name = "IsHealthy")]
            [Description("Vault is healthy")]
            public bool IsHealthy { get; set; }

            [DataMember(Name = "Info")]
            [Description("Info")]
            public VaultInfo_ Info { get; set; }

            public class VaultInfo_
            {
                [DataMember(Name = "Initialized")]
                [Description("Vault is initialized")]
                public bool Initialized { get; set; }

                [DataMember(Name = "@Sealed")]
                [Description("Vault is sealed")]
                public bool @Sealed { get; set; }

                [DataMember(Name = "StandBy")]
                [Description("Vault is in stand by")]
                public bool StandBy { get; set; }

                [DataMember(Name = "PerformanceStandby")]
                [Description("Vault is in performance stand by")]
                public bool PerformanceStandby { get; set; }

                public override string ToString()
                {
                    return $"VaultInfo_(Initialized={Initialized}; Sealed={@Sealed}; StandBy={StandBy},PerformanceStandby={PerformanceStandby})";
                }
            }
        }
        public override string ToString()
        {
            return "HealthComponent_(" + 
                "Database=" + (Database != null ? Database.ToString() : "null") + ";" +
                "Vault=" + (Vault != null ? Vault.ToString() : "null") + ";" +
            ")";
        }
    }

    public override string ToString()
    {
        return $"HealthExt(IsHealthy={IsHealthy}; Timestamp={Timestamp}; Version={Version}; Component={Component})";
    }
}