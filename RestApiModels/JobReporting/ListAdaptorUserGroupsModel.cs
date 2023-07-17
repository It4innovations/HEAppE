using HEAppE.RestApiModels.AbstractModels;
using System.Runtime.Serialization;

namespace HEAppE.RestApiModels.JobReporting
{
    [DataContract(Name = "ListAdaptorUserGroupsModel")]
    public class ListAdaptorUserGroupsModel : SessionCodeModel
    {
        public override string ToString()
        {
            return $"ListAdaptorUserGroupsModel({base.ToString()})";
        }
    }
}
