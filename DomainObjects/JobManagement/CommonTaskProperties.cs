using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;
using HEAppE.DomainObjects.JobManagement.JobInformation;

namespace HEAppE.DomainObjects.JobManagement;

public abstract class CommonTaskProperties : IdentifiableDbEntity
{
    public CommonTaskProperties()
    {
    }

    public CommonTaskProperties(CommonTaskProperties commonTaskProperties) : base(commonTaskProperties)
    {
        Name = commonTaskProperties.Name;
        MinCores = commonTaskProperties.MinCores;
        MaxCores = commonTaskProperties.MaxCores;
        Priority = commonTaskProperties.Priority;
        Project = commonTaskProperties.Project;
        WalltimeLimit = commonTaskProperties.WalltimeLimit;
        //ref types 
        EnvironmentVariables = new List<EnvironmentVariable>();
        if (commonTaskProperties.EnvironmentVariables != null)
            foreach (var envVariable in commonTaskProperties.EnvironmentVariables)
                EnvironmentVariables.Add(new EnvironmentVariable(envVariable));
    }

    [Required] [StringLength(50)] public string Name { get; set; }

    public int? MinCores { get; set; }

    public int? MaxCores { get; set; }

    public TaskPriority? Priority { get; set; }

    [ForeignKey("Project")] public long? ProjectId { get; set; }

    public virtual Project Project { get; set; }

    public int? WalltimeLimit { get; set; }

    // Objects in all related collections have to implement the ICloneable interface to support the combination with client job specification.
    public virtual List<EnvironmentVariable> EnvironmentVariables { get; set; } = new();


    public override string ToString()
    {
        var result = new StringBuilder();
        result.AppendLine("Id=" + Id);
        result.AppendLine("Name=" + Name);
        result.AppendLine("MinCores=" + MinCores);
        result.AppendLine("MaxCores=" + MaxCores);
        result.AppendLine("Priority=" + Priority);
        result.AppendLine("Project=" + Project);
        result.AppendLine("WalltimeLimit=" + WalltimeLimit);
        var i = 0;
        if (EnvironmentVariables != null)
            foreach (var variable in EnvironmentVariables)
                result.AppendLine("EnvironmentVariable" + i++ + ": " + variable);
        return result.ToString();
    }
}