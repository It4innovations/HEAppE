namespace HEAppE.DataStagingAPI.Configuration;
#nullable disable
public class DeploymentOptions
{
    /// <summary>
    ///     Instance name
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    ///     Instance description
    /// </summary>
    public string Description { get; set; }

    /// <summary>
    ///     Instance version
    /// </summary>
    public string Version { get; set; }

    /// <summary>
    ///     Instance IP
    /// </summary>
    public string DeployedIPAddress { get; set; }
}