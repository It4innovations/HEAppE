using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime.Serialization;
using HEAppE.RestApiModels.AbstractModels;

namespace HEAppE.RestApiModels.Management;

[DataContract(Name = "CreateCommandTemplateModel")]
[Description("Create command template model")]
public class CreateCommandTemplateModel : SessionCodeModel
{
    /// <summary>
    /// Name
    /// </summary>
    [DataMember(Name = "Name", IsRequired = true)]
    [StringLength(1000)]
    [Description("Name")]
    public string Name { get; set; }

    /// <summary>
    /// Description
    /// </summary>
    [DataMember(Name = "Description", IsRequired = false)]
    [StringLength(1000)]
    [Description("Description")]
    public string Description { get; set; }

    /// <summary>
    /// Extended allocation command
    /// </summary>
    [DataMember(Name = "ExtendedAllocationCommand", IsRequired = false)]
    [StringLength(1000)]
    [Description("Extended allocation command")]
    public string ExtendedAllocationCommand { get; set; }

    /// <summary>
    /// Executable file
    /// </summary>
    [DataMember(Name = "ExecutableFile", IsRequired = true)]
    [StringLength(1000)]
    [Description("Executable file")]
    public string ExecutableFile { get; set; }

    /// <summary>
    /// Preparation script
    /// </summary>
    [DataMember(Name = "PreparationScript", IsRequired = false)]
    [StringLength(1000)]
    [Description("Preparation script")]
    public string PreparationScript { get; set; }

    /// <summary>
    /// Cluster node type id
    /// </summary>
    [DataMember(Name = "ClusterNodeTypeId", IsRequired = true)]
    [Description("Cluster node type id")]
    public long ClusterNodeTypeId { get; set; }

    /// <summary>
    /// Project id
    /// </summary>
    [DataMember(Name = "ProjectId", IsRequired = true)]
    [Description("Project id")]
    public long ProjectId { get; set; }

    /// <summary>
    /// List of create command inner template parameter
    /// </summary>
    [DataMember(Name = "TemplateParameters", IsRequired = false)]
    [Description("List of create command inner template parameter")]
    public List<CreateCommandInnerTemplateParameterModel> TemplateParameters { get; set; }

    public override string ToString()
    {
        return $"CreateCommandTemplateModel({base.ToString()}; Name: {Name}; Description: {Description}; ExtendedAllocationCommand: {ExtendedAllocationCommand}; ExecutableFile: {ExecutableFile}; PreparationScript: {PreparationScript}, ClusterNodeTypeId: {ClusterNodeTypeId}, ProjectId: {ProjectId}, TemplateParameters: {string.Join(",", TemplateParameters.Select(x => x.ToString()))}";
    }
}