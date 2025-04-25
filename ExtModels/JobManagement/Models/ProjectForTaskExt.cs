using System.ComponentModel;
using System.Runtime.Serialization;
using HEAppE.ExtModels.ClusterInformation.Models;

namespace HEAppE.ExtModels.JobManagement.Models;

/// <summary>
/// Project for task model
/// </summary>
[DataContract(Name = "ProjectForTaskExt")]
[Description("Project for task model")]
public class ProjectForTaskExt
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
    /// Command template
    /// </summary>
    [DataMember(Name = "CommandTemplates")]
    [Description("Command template")]
    public CommandTemplateExt CommandTemplate { get; set; }

    #region Public methods

    public override string ToString()
    {
        return
            $"Project: Id={Id}, Name={Name}, Description={Description}, AccountingString={AccountingString}, commandTemplate={CommandTemplate}";
    }

    #endregion
}