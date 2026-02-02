using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using HEAppE.RestApiModels.AbstractModels;

namespace HEAppE.RestApiModels.Management;

/// <summary>
/// List adaptor users model
/// </summary>
[DataContract(Name = "ListAdaptorUsersModel")]
[Description("List adaptor users model")]
public class ListAdaptorUsersModel : SessionCodeModel
{
}