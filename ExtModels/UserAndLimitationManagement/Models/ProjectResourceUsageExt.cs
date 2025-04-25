using System;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace HEAppE.ExtModels.UserAndLimitationManagement.Models;

/// <summary>
/// Project resource usage ext
/// </summary>
[DataContract(Name = "ProjectResourceUsageExt")]
[Description("Project resource usage ext")]
public class ProjectResourceUsageExt
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
    /// Array of node types
    /// </summary>
    [DataMember(Name = "NodeTypes")]
    [Description("Array of node types")]
    public ClusterNodeTypeResourceUsageExt[] NodeTypes { get; set; }

    #region Public methods

    public override string ToString()
    {
        return
            $"ResourceProjectExt: Id={Id}, Name={Name}, Description={Description}, AccountingString={AccountingString}, StartDate={StartDate}, EndDate={EndDate}, NodeTypes={NodeTypes}";
    }

    #endregion
}