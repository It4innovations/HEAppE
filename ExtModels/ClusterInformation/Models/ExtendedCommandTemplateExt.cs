using System.ComponentModel;
using System.Linq;
using System.Runtime.Serialization;

namespace HEAppE.ExtModels.ClusterInformation.Models;

/// <summary>
/// Extended command template ext
/// </summary>
[DataContract(Name = "ExtendedCommandTemplateExt")]
[Description("Extended command template ext")]
public class ExtendedCommandTemplateExt
{
    /// <summary>
    /// Id
    /// </summary>
    [DataMember(Name = "Id")]
    [Description("Id")]
    public long? Id { get; set; }

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
    /// Extended allocation command
    /// </summary>
    [DataMember(Name = "ExtendedAllocationCommand")]
    [Description("Extended allocation command")]
    public string ExtendedAllocationCommand { get; set; }

    /// <summary>
    /// Executable file
    /// </summary>
    [DataMember(Name = "ExecutableFile")]
    [Description("Executable file")]
    public string ExecutableFile { get; set; }

    /// <summary>
    /// Preparation script
    /// </summary>
    [DataMember(Name = "PreparationScript")]
    [Description("Preparation script")]
    public string PreparationScript { get; set; }

    /// <summary>
    /// Command parameters
    /// </summary>
    [DataMember(Name = "CommandParameters")]
    [Description("Command parameters")]
    public string CommandParameters { get; set; }

    /// <summary>
    /// Is generic
    /// </summary>
    [DataMember(Name = "IsGeneric")]
    [Description("Is generic")]
    public bool IsGeneric { get; set; }
    
    /// <summary>
    /// Is genabled
    /// </summary>
    [DataMember(Name = "IsEnabled")]
    [Description("Is enabled")]
    public bool IsEnabled { get; set; }

    /// <summary>
    /// Project id
    /// </summary>
    [DataMember(Name = "ProjectId")]
    [Description("Project id")]
    public long ProjectId { get; set; }

    /// <summary>
    /// Cluster node type id
    /// </summary>
    [DataMember(Name = "ClusterNodeTypeId")]
    [Description("Cluster node type id")]
    public long ClusterNodeTypeId { get; set; }
    
    /// <summary>
    /// Created from generic template id
    /// </summary>
    [DataMember(Name = "CreatedFromGenericTemplateId")]
    [Description("Created from generic template id")]
    public long? CreatedFromGenericTemplateId { get; set; }

    /// <summary>
    /// Array of template parameters
    /// </summary>
    [DataMember(Name = "TemplateParameters")]
    [Description("Array of template parameters")]
    public ExtendedCommandTemplateParameterExt[] TemplateParameters { get; set; }

    public override string ToString()
    {
        return $"ExtendedCommandTemplateExt(id={Id}; name={Name}; description={Description}; extendedAllocationCommand={ExtendedAllocationCommand}; isGeneric={IsGeneric}; isEnabled={IsEnabled}; ProjectId={ProjectId}; ClusterNodeTypeId={ClusterNodeTypeId}; IsCreatedFromCommandTemplate={CreatedFromGenericTemplateId};templateParameters={string.Join(";", TemplateParameters.Select(x => x.ToString()))})";
    }
}