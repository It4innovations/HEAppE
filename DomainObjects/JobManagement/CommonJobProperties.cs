using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace HEAppE.DomainObjects.JobManagement;

public abstract class CommonJobProperties : IdentifiableDbEntity
{
    [Required] [StringLength(50)] public string Name { get; set; }

    [ForeignKey("Project")] public long ProjectId { get; set; }

    public virtual Project Project { get; set; }

    public int? WalltimeLimit { get; set; }

    // Objects in all related collections have to implement the ICloneable interface to support the combination with client job specification.
    public virtual List<EnvironmentVariable> EnvironmentVariables { get; set; } = new();

    public override string ToString()
    {
        var result = new StringBuilder();
        result.AppendLine("Id=" + Id);
        result.AppendLine("Name=" + Name);
        result.AppendLine("Project=" + Project);
        result.AppendLine("WalltimeLimit=" + WalltimeLimit);
        var i = 0;
        if (EnvironmentVariables != null)
            foreach (var variable in EnvironmentVariables)
                result.AppendLine("EnvironmentVariable" + i++ + ": " + variable);
        return result.ToString();
    }
}