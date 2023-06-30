using FluentValidation;
using HEAppE.RestApiModels.AbstractModels;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace HEAppE.RestApiModels.FileTransfer
{
    [DataContract(Name = "DownloadFileFromClusterModel")]
    public class DownloadFileFromClusterModel : SubmittedJobInfoModel
    {
        [DataMember(Name = "RelativeFilePath"), StringLength(50)]
        public string RelativeFilePath { get; set; }
        public override string ToString()
        {
            return $"DownloadFileFromClusterModel({base.ToString()}; RelativeFilePath: {RelativeFilePath})";
        }

    }
    public class DownloadFileFromClusterModelValidator : AbstractValidator<DownloadFileFromClusterModel>
    {
        public DownloadFileFromClusterModelValidator()
        {
            Include(new SubmittedJobInfoModelValidator());
        }
    }
}
