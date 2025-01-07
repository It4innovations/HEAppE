using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HEAppE.DomainObjects.JobManagement;

[Table("EnvironmentVariable")]
public class EnvironmentVariable : IdentifiableDbEntity, ICloneable
{
    public EnvironmentVariable()
    {
    }

    public EnvironmentVariable(EnvironmentVariable environment) : base(environment)
    {
        Name = environment.Name;
        Value = environment.Value;
    }

    [Required] [StringLength(50)] public string Name { get; set; }

    //[Required]
    [StringLength(100)] public string Value { get; set; }

    #region ICloneable Members

    public object Clone()
    {
        return new EnvironmentVariable
        {
            Name = Name,
            Value = Value
        };
    }

    #endregion

    public override string ToString()
    {
        return string.Format("EnvironmentVariable: Id={0}, Name={1}, Value={2}", Id, Name, Value);
    }
}