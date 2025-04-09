namespace HEAppE.DataAccessTier.Vault.Settings;

public class VaultConnectorSettings
{
    #region Properties
    /// <summary>
    /// Vault base address for the connection (with port)
    /// </summary>
    public static string VaultBaseAddress { get; set; } = "http://vaultagent:8100";
    
    /// <summary>
    /// Cluster authentication credentials path
    /// </summary>
    public static string ClusterAuthenticationCredentialsPath { get; set; } = "v1/HEAppE/data/ClusterAuthenticationCredentials";
    
    #endregion
}