using System;
using System.ComponentModel;
using System.Runtime.Serialization;
using HEAppE.ExtModels.ClusterInformation.Models;

namespace HEAppE.ExtModels.JobManagement.Models;

/// <summary>
/// Extended project info ext
/// </summary>
[DataContract(Name = "ExtendedProjectInfoExt")]
[Description("Extended project info ext")]
public class ExtendedProjectInfoExt
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
    /// Primary investigator contact
    /// </summary>
    [DataMember(Name = "PrimaryInvestigatorContact")]
    [Description("Primary investigator contact")]
    public string PrimaryInvestigatorContact { get; set; }

    /// <summary>
    /// Array of contacts
    /// </summary>
    [DataMember(Name = "Contacts")]
    [Description("Array of contacts")]
    public string[] Contacts { get; set; }

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