using System;
using System.ComponentModel;
using System.Runtime.Serialization;
using HEAppE.ExtModels.ClusterInformation.Models;
using HEAppE.ExtModels.JobReporting.Models;
using HEAppE.ExtModels.Management.Models;

namespace HEAppE.ExtModels.JobManagement.Models;

/// <summary>
/// Project ext
/// </summary>
[DataContract(Name = "ProjectExt")]
[Description("Project ext")]
public class ProjectExt
{
    /// <summary>
    /// Id
    /// </summary>
    [DataMember(Name = "Id")]
    [Description("Id")]
    public long Id { get; set; }

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
    /// Accounting string
    /// </summary>
    [DataMember(Name = "AccountingString")]
    [Description("Accounting string")]
    public string AccountingString { get; set; }

    /// <summary>
    /// Start date
    /// </summary>
    [DataMember(Name = "StartDate")]
    [Description("Start date")]
    public DateTime StartDate { get; set; }

    /// <summary>
    /// End date
    /// </summary>
    [DataMember(Name = "EndDate")]
    [Description("End date")]
    public DateTime EndDate { get; set; }

    /// <summary>
    /// Usage type
    /// </summary>
    [DataMember(Name = "UsageType")]
    [Description("Usage type")]
    public UsageTypeExt UsageType { get; set; }

    /// <summary>
    /// Use accounting string for scheduler
    /// </summary>
    [DataMember(Name = "UseAccountingStringForScheduler")]
    [Description("Use accounting string for scheduler")]
    public bool UseAccountingStringForScheduler { get; set; }


    /// <summary>
    /// Map user account to exact robot account
    /// </summary>
    [DataMember(Name = "IsOneToOneMapping")]
    [Description("Map user account to exact robot account")]
    public bool IsOneToOneMapping { get; set; }
    
    /// <summary>
    /// Key scripts directory path
    /// </summary>
    [DataMember(Name = "KeyScriptsDirectoryPath")]
    [Description("Key scripts directory path")]
    public string KeyScriptsDirectoryPath { get; set; }

    /// <summary>
    /// Array of command templates
    /// </summary>
    [DataMember(Name = "CommandTemplates")]
    [Description("Array of command templates")]
    public CommandTemplateExt[] CommandTemplates { get; set; }
    

    #region Public methods

    public override string ToString()
    {
        return
            $"Project: Id={Id}, Name={Name}, Description={Description}, AccountingString={AccountingString}, StartDate={StartDate}, EndDate={EndDate}, commandTemplates={CommandTemplates}";
    }

    #endregion
}