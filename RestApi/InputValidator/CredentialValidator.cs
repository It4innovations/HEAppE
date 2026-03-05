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
        switch(ext.AuthType)
        {
            case ClusterAuthenticationCredentialsAuthType.Kerberos:
                if(ext.GenerateNewKey is true)
                    _messageBuilder.AppendLine("GenerateNewKey is not applicable for Kerberos credentials.");
                
                if(!string.IsNullOrEmpty(ext.ProvidedPrivateKey))
                    _messageBuilder.AppendLine("ProvidedPrivateKey is not applicable for Kerberos credentials.");

                if(!string.IsNullOrEmpty(ext.Passphrase))
                    _messageBuilder.AppendLine("Passphrase is not applicable for Kerberos credentials.");
                
                if(!string.IsNullOrEmpty(ext.Password))
                    _messageBuilder.AppendLine("Password is not applicable for Kerberos credentials.");
                
                break;
            
            case ClusterAuthenticationCredentialsAuthType.PasswordAndPrivateKey:
                if(ext.GenerateNewKey is true && string.IsNullOrEmpty(ext.ProvidedPrivateKey))
                    _messageBuilder.AppendLine("ProvidedPrivateKey cannot be null or empty when GenerateNewKey is true, for PasswordAndPrivateKey Authentication.");

                if(string.IsNullOrEmpty(ext.Password))
                    _messageBuilder.AppendLine("Password cannot be null or empty for PasswordAndPrivateKey Authentication.");

                //TODO: what about Passphrase?
                //if(string.IsNullOrEmpty(ext.Passphrase))
                //    _messageBuilder.AppendLine("Passphrase cannot be null or empty for PasswordAndPrivateKey Authentication.");

                break;

            case ClusterAuthenticationCredentialsAuthType.PrivateKey:
                if(ext.GenerateNewKey is true && string.IsNullOrEmpty(ext.ProvidedPrivateKey))
                    _messageBuilder.AppendLine("ProvidedPrivateKey cannot be null or empty when GenerateNewKey is true, for PrivateKey Authentication.");

                break;
            
            default:
                _messageBuilder.AppendLine($"{ext.AuthType} Authentication type not supported.");
                break;
        }

        return _messageBuilder.ToString();
    }
}