using System.ComponentModel;
using System.Runtime.Serialization;

namespace HEAppE.ExtModels.ClusterInformation.Models;

/// <summary>
/// Extended command template parameter ext
/// </summary>
[DataContract(Name = "ExtendedCommandTemplateParameterExt")]
[Description("Extended command template parameter ext")]
public class ExtendedCommandTemplateParameterExt
{
    /// <summary>
    /// Id
    /// </summary>
    [DataMember(Name = "Id")]
    [Description("Id")]
    public long Id { get; set; }

    /// <summary>
    /// Identifier
    /// </summary>
    [DataMember(Name = "Identifier")]
    [Description("Identifier")]
    public string Identifier { get; set; }

    /// <summary>
    /// Description
    /// </summary>
    [DataMember(Name = "Description")]
    [Description("Description")]
    public string Description { get; set; }

    /// <summary>
    /// Query
    /// </summary>
    [DataMember(Name = "Query")]
    [Description("Query")]
    public string Query { get; set; }

    public override string ToString()
    {
        return $"ExtendedCommandTemplateParameterExt(Id: {Id}, Identifier:{Identifier}, Description: {Description}, Query: {Query})";
    }
}