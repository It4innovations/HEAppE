using HEAppE.RestApiModels.AbstractModels;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace HEAppE.RestApiModels.ClusterInformation
{
    [DataContract(Name = "GetCommandTemplateParametersNameModele")]
    public class GetCommandTemplateParametersNameModel : SessionCodeModel
    {
        [DataMember(Name = "CommandTemplateId")]
        public long CommandTemplateId { get; set; }

        [DataMember(Name = "UserScriptPath", IsRequired = false), StringLength(250)]
        public string UserScriptPath { get; set; }
        public override string ToString()
        {
            return $"GetCommandTemplateParametersNameModel({base.ToString()}; CommandTemplateId: {CommandTemplateId}; UserScriptPath: {UserScriptPath})";
        }
    }
}
