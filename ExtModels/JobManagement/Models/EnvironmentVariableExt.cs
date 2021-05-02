using System;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace HEAppE.ExtModels.JobManagement.Models
{
    [DataContract(Name = "EnvironmentVariableExt")]
    public class EnvironmentVariableExt
    {
        [DataMember(Name ="Name"), StringLength(50)]
        public string Name { get; set; }

        [DataMember(Name = "Value"), StringLength(100)]
        public string Value { get; set; }

        public override string ToString()
        {
            return $"EnvironmentVariableExt(name={Name}; value={Value})";
        }
    }
}
