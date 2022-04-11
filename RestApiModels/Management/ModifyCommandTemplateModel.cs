using HEAppE.RestApiModels.AbstractModels;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace HEAppE.RestApiModels.Management
{
    [DataContract(Name = "ModifyCommandTemplateModel")]
    public class ModifyCommandTemplateModel : SessionCodeModel
    {
		[DataMember(Name = "CommandTemplateId", IsRequired = true)]
		public long CommandTemplateId { get; set; }

		[DataMember(Name= "Name", IsRequired = false), StringLength(30)]
		public string Name { get; set; }

		[DataMember(Name = "Description", IsRequired = false), StringLength(200)]
		public string Description { get; set; }

		[DataMember(Name = "Code", IsRequired = false), StringLength(50)]
		public string Code { get; set; }

		[DataMember(Name = "ExecutableFile", IsRequired = false), StringLength(100)]
		public string ExecutableFile { get; set; }

		[DataMember(Name = "PreparationScript", IsRequired = false), StringLength(1000)]
		public string PreparationScript { get; set; }
	}
}
