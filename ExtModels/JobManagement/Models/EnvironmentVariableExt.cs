using System;
using System.Runtime.Serialization;

namespace HEAppE.ExtModels.JobManagement.Models
{
    [DataContract(Name = "EnvironmentVariableExt")]
    public class EnvironmentVariableExt
    {
        [DataMember(Name ="Name")]
        public string Name { get; set; }

        [DataMember(Name = "Value")]
        public string Value { get; set; }

        public override string ToString()
        {
            return $"EnvironmentVariableExt(name={Name}; value={Value})";
        }
    }
}
