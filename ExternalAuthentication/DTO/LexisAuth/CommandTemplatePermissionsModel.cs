using System;
using System.Collections.Generic;

namespace HEAppE.ExternalAuthentication.DTO.LexisAuth;

public class CommandTemplatePermissionsModel
{
    public string HeappeIdentifier { get; set; }
    public List<ProjectPermissionModel> Permissions { get; set; }
}

public class ProjectPermissionModel
{
    public string QueueName { get; set; }
    public string ProjectResource { get; set; }
    public string ClusterName { get; set; }
    public string ProjectShortName { get; set; }
    public List<CommandTemplateModel> CommandTemplates { get; set; }
}

public class CommandTemplateModel
{
    public string Name { get; set; }
    public bool Enabled { get; set; }
}