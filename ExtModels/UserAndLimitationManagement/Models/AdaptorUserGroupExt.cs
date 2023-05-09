using HEAppE.ExtModels.JobManagement.Models;
using System;
using System.Runtime.Serialization;

namespace HEAppE.ExtModels.UserAndLimitationManagement.Models
{
    [DataContract(Name = "AdaptorUserGroupExt")]
    public class AdaptorUserGroupExt
    {
        [DataMember(Name = "Id")]
        public long? Id { get; set; }

        [DataMember(Name = "Name")]
        public string Name { get; set; }

        [DataMember(Name = "Description")]
        public string Description { get; set; }

        [DataMember(Name = "Project")]
        public ProjectExt Project { get; set; }

        [DataMember(Name = "Users")]
        public AdaptorUserExt[] Users { get; set; }

        public override string ToString()
        {
            return $"AdaptorUserGroupExt(id={Id}; name={Name}; description={Description}; Project={Project}; users={Users})";
        }
    }
}
