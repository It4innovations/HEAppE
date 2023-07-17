using HEAppE.ExtModels.ClusterInformation.Models;
using System.Runtime.Serialization;

namespace HEAppE.ExtModels.JobManagement.Models
{
    [DataContract(Name = "ProjectForTaskExt")]
    public class ProjectForTaskExt
    {
        [DataMember(Name = "Id")]
        public long Id { get; set; }

        [DataMember(Name = "Name")]
        public string Name { get; set; }

        [DataMember(Name = "Description")]
        public string Description { get; set; }

        [DataMember(Name = "AccountingString")]
        public string AccountingString { get; set; }

        [DataMember(Name = "CommandTemplates")]
        public CommandTemplateExt CommandTemplate { get; set; }

        #region Public methods
        public override string ToString()
        {
            return $"Project: Id={Id}, Name={Name}, Description={Description}, AccountingString={AccountingString}, commandTemplate={CommandTemplate}";
        }
        #endregion
    }
}
