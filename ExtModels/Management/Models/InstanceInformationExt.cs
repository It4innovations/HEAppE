using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Serialization;
using HEAppE.ExtModels.JobManagement.Models;

namespace HEAppE.ExtModels.Management.Models;

/// <summary>
/// Instance information ext
/// </summary>
[DataContract(Name = "InstanceInformationExt")]
[Description("Instance information ext")]
public class InstanceInformationExt
{
    #region Override methods

    /// <summary>
    ///     Override to string method
    /// </summary>
    /// <returns></returns>
    public override string ToString()
    {
        return
            $"InstanceInformationExt: Name={Name}, Description={Description}, Version={Version}, DeployedIPAddress={DeployedIPAddress}, Port={Port}, URL={URL}, URLPostfix={URLPostfix}, DeploymetType={DeploymentType}, ResourceAllocationTypes={ResourceAllocationTypes}, Projects={Projects}";
    }

    #endregion

    #region Properties

    /// <summary>
    /// Name
    /// </summary>
    [DataMember(Name = "Name")]
    [Description("Name")]
    public string Name { get; set; }

    /// <summary>
    /// Description
    /// </summary>
    [DataMember(Name = "Description")]
    [Description("Description")]
    public string Description { get; set; }

    /// <summary>
    /// Version
    /// </summary>
    [DataMember(Name = "Version")]
    [Description("Version")]
    public string Version { get; set; }

    /// <summary>
    /// Deployed IP address
    /// </summary>
    [DataMember(Name = "DeployedIPAddress")]
    [Description("Deployed IP address")]
    public string DeployedIPAddress { get; set; }

    /// <summary>
    /// Port
    /// </summary>
    [DataMember(Name = "Port")]
    [Description("Port")]
    public int Port { get; set; }

    /// <summary>
    /// URL
    /// </summary>
    [DataMember(Name = "URL")]
    [Description("URL")]
    public string URL { get; set; }

    /// <summary>
    /// URL Postfix
    /// </summary>
    [DataMember(Name = "URLPostfix")]
    [Description("URL Postfix")]
    public string URLPostfix { get; set; }

    /// <summary>
    /// Deployment type
    /// </summary>
    [DataMember(Name = "DeploymentType")]
    [Description("Deployment type")]
    public DeploymentTypeExt DeploymentType { get; set; }

    /// <summary>
    /// Deployment allocation types
    /// </summary>
    [DataMember(Name = "ResourceAllocationTypes")]
    [Description("Deployment allocation types")]
    public IEnumerable<ResourceAllocationTypeExt> ResourceAllocationTypes { get; set; }

    /// <summary>
    /// Instance reference to HPC Projects
    /// </summary>
    [DataMember(Name = "Projects")]
    [Description("Instance reference to HPC Projects")]
    public IEnumerable<ExtendedProjectInfoExt> Projects { get; set; }

    #endregion
}