using System.ComponentModel;
using System.Linq;
using System.Runtime.Serialization;

namespace HEAppE.ExtModels.ClusterInformation.Models;

/// <summary>
/// Command template ext
/// </summary>
[DataContract(Name = "CommandTemplateExt")]
[Description("Command template ext")]
public class CommandTemplateExt
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
    /// Is generic
    /// </summary>
    [DataMember(Name = "IsGeneric")]
    [Description("Is generic")]
    public bool IsGeneric { get; set; }

    /// <summary>
    /// Is enabled
    /// </summary>
    [DataMember(Name = "IsEnabled")]
    [Description("Is enabled")]
    public bool IsEnabled { get; set; }
    
    /// <summary>
    /// Created from generic template id
    /// </summary>
    [DataMember(Name = "CreatedFromGenericTemplateId")]
    [Description("Created from generic template id")]
    public long? CreatedFromGenericTemplateId { get; set; }

    /// <summary>
    /// Array of command template parameters
    /// </summary>
    [DataMember(Name = "TemplateParameters")]
    [Description("Array of command template parameters")]
    public CommandTemplateParameterExt[] TemplateParameters { get; set; }

    public override string ToString()
    {
        return
            $"CommandTemplateExt(id={Id}; name={Name}; description={Description}; extendedAllocationCommand={ExtendedAllocationCommand}; isGeneric={IsGeneric}; isEnabled={IsEnabled}; createdFromGenericTemplateId={CreatedFromGenericTemplateId}; templateParameters={string.Join(";", TemplateParameters.Select(x => x.ToString()))})";
    }
}