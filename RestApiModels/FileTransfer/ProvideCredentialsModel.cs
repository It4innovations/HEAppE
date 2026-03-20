using System.ComponentModel;
using System.Runtime.Serialization;
using FluentValidation;
using HEAppE.RestApiModels.AbstractModels;

namespace HEAppE.RestApiModels.FileTransfer;

/// <summary>
/// Model for retrieving file transfer method
/// </summary>
[DataContract(Name = "ProvideCredentials")]
[Description("Model for providing credentials for file transfer")]
public class ProvideCredentialsModel
{
    public long ProjectId { get; set; }
    public long ClusterId { get; set; }
    public override string ToString()
    {
        return $"ProvideCredentialsModel(ProjectId: {ProjectId}, ClusterId: {ClusterId})";
    }

    public class ProvideCredentialsModelValidator : AbstractValidator<ProvideCredentialsModel>
    {
        public ProvideCredentialsModelValidator()
        {
            RuleFor(x => x.ProjectId).GreaterThan(0);
            RuleFor(x => x.ClusterId).GreaterThan(0);
        }
    }
}