using HEAppE.DomainObjects.ClusterInformation;
using HEAppE.RestApiModels.Management;
using HEAppE.Utils.Validation;

namespace HEAppE.RestApi.InputValidator;

public class CredentialValidator : AbstractValidator
{
    public CredentialValidator(object validationObj) : base(validationObj)
    {
    }

    public override ValidationResult Validate()
    {
        var message = _validationObject switch
        {
            CreateCredentialModel ext => ValidateCreateCredentialModel(ext),
            _ => string.Empty
        };

        return new ValidationResult(string.IsNullOrEmpty(message), message);
    }

    private string ValidateCreateCredentialModel(CreateCredentialModel ext)
    {
        var sessionCodeValidation = new SessionCodeValidator(ext.SessionCode).Validate();
        if (!sessionCodeValidation.IsValid) 
            _messageBuilder.AppendLine(sessionCodeValidation.Message);

        ValidateId(ext.ProjectId, "ProjectId");
        
        if(ext.AuthType == ClusterAuthenticationCredentialsAuthType.Kerberos)
        {
            if(!string.IsNullOrEmpty(ext.Passphrase))
                _messageBuilder.AppendLine("Passphrase is not applicable for Kerberos credentials.");
        }

        return _messageBuilder.ToString();
    }
}