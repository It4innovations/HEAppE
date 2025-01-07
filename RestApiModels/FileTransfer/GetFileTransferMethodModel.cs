using System.Runtime.Serialization;
using FluentValidation;
using HEAppE.RestApiModels.AbstractModels;

namespace HEAppE.RestApiModels.FileTransfer;

[DataContract(Name = "GetFileTransferMethodModel")]
public class GetFileTransferMethodModel : SubmittedJobInfoModel
{
    public override string ToString()
    {
        return $"GetFileTransferMethodModel({base.ToString()})";
    }

    public class GetFileTransferMethodModelValidator : AbstractValidator<GetFileTransferMethodModel>
    {
        public GetFileTransferMethodModelValidator()
        {
            Include(new SubmittedJobInfoModelValidator());
        }
    }
}