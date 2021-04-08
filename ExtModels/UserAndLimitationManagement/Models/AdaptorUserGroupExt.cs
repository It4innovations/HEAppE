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

        [DataMember(Name = "AccountingString")]
        public string AccountingString { get; set; }

        [DataMember(Name = "Users")]
        public AdaptorUserExt[] Users { get; set; }

        public override string ToString()
        {
            return $"AdaptorUserGroupExt(id={Id}; name={Name}; description={Description}; accountingString={AccountingString}; users={Users})";
        }
    }
}
