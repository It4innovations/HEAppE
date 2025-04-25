using System.ComponentModel;
using System.Runtime.Serialization;
using FluentValidation;
using HEAppE.RestApiModels.AbstractModels;

namespace HEAppE.RestApiModels.FileTransfer;

/// <summary>
/// Model for retrieving file transfer method
/// </summary>
[DataContract(Name = "GetFileTransferMethodModel")]
[Description("Model for retrieving file transfer method")]
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