﻿using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using HEAppE.RestApiModels.AbstractModels;

namespace HEAppE.RestApiModels.Management;

[DataContract(Name = "ModifyProjectAssignmentToClusterModel")]
public class ModifyProjectAssignmentToClusterModel : SessionCodeModel
{
    [DataMember(Name = "ProjectId", IsRequired = true)]
    public long ProjectId { get; set; }

    [DataMember(Name = "ClusterId", IsRequired = true)]
    public long ClusterId { get; set; }

    [DataMember(Name = "LocalBasepath", IsRequired = true)]
    [StringLength(100)]
    public string LocalBasepath { get; set; }
}