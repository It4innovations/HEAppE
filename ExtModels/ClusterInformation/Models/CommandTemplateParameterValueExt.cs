using System.Runtime.Serialization;

namespace HEAppE.ExtModels.ClusterInformation.Models
{
    [DataContract(Name = "CommandTemplateParameterValueExt")]
    public class CommandTemplateParameterValueExt
    {
        [DataMember(Name = "CommandParameterIdentifier")]
        public string CommandParameterIdentifier { get; set; }

        [DataMember(Name = "ParameterValue")]
        public string ParameterValue { get; set; }

        public override string ToString()
        {
            return $"CommandTemplateParameterValueExt(commandParameterIdentifier={CommandParameterIdentifier}; parameterValue={ParameterValue})";
        }
    }
}
