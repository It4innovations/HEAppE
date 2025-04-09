using System.ComponentModel;
using System.Runtime.Serialization;

namespace HEAppE.ExtModels.Management.Models;

/// <summary>
/// Instance information ext
/// </summary>
[DataContract(Name = "VersionInformationExt")]
[Description("Instance information ext")]
public class VersionInformationExt
{
    #region Override methods

    /// <summary>
    ///     Override to string method
    /// </summary>
    /// <returns></returns>
    public override string ToString()
    {
        return $"InstanceInformationExt: Name={Name}, Description={Description}, Version={Version}";
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

    #endregion
}