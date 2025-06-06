﻿using HEAppE.ExtModels.FileTransfer.Models;
using HEAppE.ExtModels.UserAndLimitationManagement.Models;
using HEAppE.Utils.Validation;

namespace HEAppE.RestApi.InputValidator;

public class CredentialsValidator : AbstractValidator
{
    public CredentialsValidator(object validationObj) : base(validationObj)
    {
    }

    private int UsernameMaxLength => 50;

    public override ValidationResult Validate()
    {
        var message = _validationObject switch
        {
            PasswordCredentialsExt ext => ValidateUserPasswordCredentials(ext),
            OpenIdCredentialsExt ext => ValidateOpenIdCredentials(ext),
            DigitalSignatureCredentialsExt ext => ValidateDigitalSignatureCredentials(ext),
            FileTransferKeyCredentialsExt ext => ValidateAsymmetricKeyCredentials(ext),
            _ => string.Empty
        };

        return new ValidationResult(string.IsNullOrEmpty(message), message);
    }

    private string ValidateAsymmetricKeyCredentials(FileTransferKeyCredentialsExt credentials)
    {
        if (string.IsNullOrEmpty(credentials.Username))
        {
            _messageBuilder.AppendLine("Username cannot be empty.");
        }
        else
        {
            if (ContainsIllegalCharacters(credentials.Username))
                _messageBuilder.AppendLine("Username contains illegal characters.");
            if (credentials.Username.Length > UsernameMaxLength)
                _messageBuilder.AppendLine($"Username is too long, maximal length is {UsernameMaxLength}");
        }

        if (string.IsNullOrEmpty(credentials.PrivateKey)) _messageBuilder.AppendLine("PrivateKey cannot be empty.");

        if (string.IsNullOrEmpty(credentials.PublicKey)) _messageBuilder.AppendLine("PublicKey cannot be empty.");

        return _messageBuilder.ToString();
    }

    private string ValidateDigitalSignatureCredentials(DigitalSignatureCredentialsExt credentials)
    {
        if (string.IsNullOrEmpty(credentials.Username))
        {
            _messageBuilder.AppendLine("Username cannot be empty.");
        }
        else
        {
            if (ContainsIllegalCharacters(credentials.Username))
                _messageBuilder.AppendLine("Username contains illegal characters.");
            if (credentials.Username.Length > UsernameMaxLength)
                _messageBuilder.AppendLine($"Username is too long, maximal length is {UsernameMaxLength}");
        }

        if (string.IsNullOrEmpty(credentials.Noise))
            _messageBuilder.AppendLine("Noise cannot be empty.");
        else if (ContainsIllegalCharacters(credentials.Noise))
            _messageBuilder.AppendLine("Noise contains illegal characters.");

        if (credentials.DigitalSignature == null) _messageBuilder.AppendLine("DigitalSignature cannot be empty.");

        return _messageBuilder.ToString();
    }

    private string ValidateOpenIdCredentials(OpenIdCredentialsExt credentials)
    {
        if (string.IsNullOrEmpty(credentials.OpenIdAccessToken))
            _messageBuilder.AppendLine("OpenIdAccessToken cannot be empty.");
        else if (ContainsIllegalCharacters(credentials.OpenIdAccessToken))
            _messageBuilder.AppendLine("OpenIdAccessToken contains illegal characters.");

        return _messageBuilder.ToString();
    }

    private string ValidateUserPasswordCredentials(PasswordCredentialsExt credentials)
    {
        if (string.IsNullOrEmpty(credentials.Username))
        {
            _messageBuilder.AppendLine("Username cannot be empty.");
        }
        else
        {
            if (ContainsIllegalCharacters(credentials.Username))
                _messageBuilder.AppendLine("Username contains illegal characters.");
            if (credentials.Username.Length > UsernameMaxLength)
                _messageBuilder.AppendLine($"Username is too long, maximal length is {UsernameMaxLength}");
        }

        if (string.IsNullOrEmpty(credentials.Password)) _messageBuilder.AppendLine("Password cannot be empty.");


        return _messageBuilder.ToString();
    }
}