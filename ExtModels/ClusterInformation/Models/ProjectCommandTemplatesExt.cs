using HEAppE.ExtModels.JobManagement.Models;
using System.Runtime.Serialization;

namespace HEAppE.ExtModels.ClusterInformation.Models
{
    [DataContract(Name = "ProjectCommandTemplatesExt")]
    public class ProjectCommandTemplatesExt
    {
        [DataMember(Name = "Project")]
        public ProjectExt Project { get; set; }

        [DataMember(Name = "CommandTemplates")]
        public CommandTemplateExt[] CommandTemplates { get; set; }

        public override string ToString()
        {
            return $"ProjectCommandTemplatesExt(project={Project}; commandTemplates={CommandTemplates})";
        }
    }
}