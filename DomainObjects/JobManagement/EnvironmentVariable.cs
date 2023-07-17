using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HEAppE.DomainObjects.JobManagement
{
    [Table("EnvironmentVariable")]
    public class EnvironmentVariable : IdentifiableDbEntity, ICloneable
    {
        [Required]
        [StringLength(50)]
        public string Name { get; set; }

        //[Required]
        [StringLength(100)]
        public string Value { get; set; }

        public EnvironmentVariable() : base() { }
        public EnvironmentVariable(EnvironmentVariable environment) : base(environment)
        {
            this.Name = environment.Name;
            this.Value = environment.Value;
        }

        public override string ToString()
        {
            return String.Format("EnvironmentVariable: Id={0}, Name={1}, Value={2}", Id, Name, Value);
        }

        #region ICloneable Members

        public object Clone()
        {
            return new EnvironmentVariable()
            {
                Name = this.Name,
                Value = this.Value
            };
        }

        #endregion
    }
}