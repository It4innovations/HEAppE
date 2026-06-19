namespace HEAppE.DataStagingAPI.Configuration;
#nullable disable
public class DeploymentOptions
{
    /// <summary>
    ///     Instance name
    /// </summary>
    public string Name { get; set; } = "HEAppE instance";
    public string Description { get; set; } = "HEAppE instance";
    public string Version { get; set; } = VersionInfo.Version;
    public string DeployedIPAddress { get; set; } = "127.0.0.1";
}