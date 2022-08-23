using HEAppE.RestApiModels.AbstractModels;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace HEAppE.RestApiModels.Management
{
    [DataContract(Name = "CreateCommandTemplateModel")]
    public class CreateCommandTemplateModel : SessionCodeModel
    {
		[DataMember(Name = "GenericCommandTemplateId", IsRequired = true)]
		public long GenericCommandTemplateId { get; set; }

		[DataMember(Name= "Name", IsRequired = true), StringLength(30)]
		public string Name { get; set; }

		[DataMember(Name = "Description", IsRequired = true), StringLength(200)]
		public string Description { get; set; }

		[DataMember(Name = "Code", IsRequired = true), StringLength(50)]
		public string Code { get; set; }

		[DataMember(Name = "ExecutableFile", IsRequired = true), StringLength(100)]
		public string ExecutableFile { get; set; }

		[DataMember(Name = "PreparationScript", IsRequired = false), StringLength(1000)]
		public string PreparationScript { get; set; }
		public override string ToString()
		{
			return $"CreateCommandTemplateModel({base.ToString()}; GenericCommandTemplateId: {GenericCommandTemplateId}; Name: {Name}; Description: {Description}; Code: {Code}; ExecutableFile: {ExecutableFile}; PreparationScript: {PreparationScript})";
		}
	}
}
