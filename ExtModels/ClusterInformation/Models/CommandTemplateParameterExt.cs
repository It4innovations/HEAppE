using System.Runtime.Serialization;

namespace HEAppE.ExtModels.ClusterInformation.Models;

[DataContract(Name = "CommandTemplateParameterExt")]
public class CommandTemplateParameterExt
{
    [DataMember(Name = "Identifier")] public string Identifier { get; set; }

    [DataMember(Name = "Description")] public string Description { get; set; }

    public override string ToString()
    {
        return $"CommandTemplateParameterExt(identifier={Identifier}; description={Description})";
    }
}