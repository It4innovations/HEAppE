using System;
using System.Collections.Generic;
using System.Linq;
using HEAppE.DomainObjects.ClusterInformation;
using HEAppE.RestApiModels.Management;
using HEAppE.Utils.Validation;

namespace HEAppE.RestApi.InputValidator;

public class CredentialValidator : AbstractValidator
{
    private enum Requirement
    {
        Optional = 0,
        Forbidden = 1,
        Required = 2,
        RequiredConditional = 3 // this code assumes the corresponding field is of type string.
    }

    private struct Conditional(string field, object value)
    {
        public string Field {get; private set;} = field;
        public object Value {get; private set;} = value;
    }

    private struct RulesDefinition
    {
        private Dictionary<string, Requirement> _rules;
        private Dictionary<string, List<Conditional>> _conditionals;

        public RulesDefinition()
        { _rules = []; }

        public void AddRule(string key, Requirement req, List<Conditional> condRules=null)
        {
            _rules.Add(key, req);
            if(req == Requirement.RequiredConditional && condRules == null)
                throw new Exception("Error: RequiredConditional needs additional rule. \"condRule\" parameter cannot be null.");
            
            _conditionals.Add(key, condRules);
        }

        public List<string> GetFields()
        {
            return _rules.Keys.ToList();
        }

        public Requirement GetRequirement(string key)
        {
            return _rules[key];
        }

        public bool ConditionsHold(string key, object obj)
        {
            bool res = true;
            foreach(Conditional conditional in _conditionals[key])
            {
                res &= (conditional.Value == obj.GetType().GetProperty(conditional.Field).GetValue(obj));
            }
            return res;
        }
    }

    static readonly Dictionary<ClusterAuthenticationCredentialsAuthType, RulesDefinition> _validationRules;

    static CredentialValidator()
    {
        #region Rules assignment
        _validationRules = new();

        var passwordRules = new RulesDefinition();
        passwordRules.AddRule("Password", Requirement.Required);
        passwordRules.AddRule("ProvidedPrivateKey", Requirement.Forbidden);
        passwordRules.AddRule("Passphrase", Requirement.Forbidden);
        passwordRules.AddRule("GenerateNewKey", Requirement.Forbidden);
        _validationRules.Add(ClusterAuthenticationCredentialsAuthType.Password, passwordRules);

        var passwordInteractiveRules = new RulesDefinition();
        passwordInteractiveRules.AddRule("Password", Requirement.Optional);
        passwordInteractiveRules.AddRule("ProvidedPrivateKey", Requirement.Forbidden);
        passwordInteractiveRules.AddRule("Passphrase", Requirement.Forbidden);
        passwordInteractiveRules.AddRule("GenerateNewKey", Requirement.Forbidden);
        _validationRules.Add(ClusterAuthenticationCredentialsAuthType.PasswordInteractive, passwordInteractiveRules);

        var passwordAndPrivateKeyRules = new RulesDefinition();
        passwordAndPrivateKeyRules.AddRule("Password", Requirement.Required);
        passwordAndPrivateKeyRules.AddRule("ProvidedPrivateKey", Requirement.RequiredConditional, new(){new Conditional("GenerateNewKey", true)});
        passwordAndPrivateKeyRules.AddRule("Passphrase", Requirement.Optional);
        passwordAndPrivateKeyRules.AddRule("GenerateNewKey", Requirement.Optional);
        _validationRules.Add(ClusterAuthenticationCredentialsAuthType.PasswordAndPrivateKey, passwordAndPrivateKeyRules);

        var privateKeyRules = new RulesDefinition();
        privateKeyRules.AddRule("Password", Requirement.Forbidden);
        privateKeyRules.AddRule("ProvidedPrivateKey", Requirement.RequiredConditional, new(){new Conditional("GenerateNewKey", true)});
        privateKeyRules.AddRule("Passphrase", Requirement.Optional);
        privateKeyRules.AddRule("GenerateNewKey", Requirement.Optional);
        _validationRules.Add(ClusterAuthenticationCredentialsAuthType.PrivateKey, privateKeyRules);

        var passwordViaProxyRules = new RulesDefinition();
        passwordViaProxyRules.AddRule("Password", Requirement.Required);
        passwordViaProxyRules.AddRule("ProvidedPrivateKey", Requirement.Forbidden);
        passwordViaProxyRules.AddRule("Passphrase", Requirement.Forbidden);
        passwordViaProxyRules.AddRule("GenerateNewKey", Requirement.Forbidden);
        _validationRules.Add(ClusterAuthenticationCredentialsAuthType.PasswordViaProxy, passwordViaProxyRules);

        var passwordInteractiveViaProxyRules = new RulesDefinition();
        passwordInteractiveViaProxyRules.AddRule("Password", Requirement.Optional);
        passwordInteractiveViaProxyRules.AddRule("ProvidedPrivateKey", Requirement.Forbidden);
        passwordInteractiveViaProxyRules.AddRule("Passphrase", Requirement.Forbidden);
        passwordInteractiveViaProxyRules.AddRule("GenerateNewKey", Requirement.Forbidden);
        _validationRules.Add(ClusterAuthenticationCredentialsAuthType.PasswordInteractiveViaProxy, passwordInteractiveViaProxyRules);

        var passwordAndPrivateKeyViaProxyRules = new RulesDefinition();
        passwordAndPrivateKeyViaProxyRules.AddRule("Password", Requirement.Required);
        passwordAndPrivateKeyViaProxyRules.AddRule("ProvidedPrivateKey", Requirement.RequiredConditional, new(){new Conditional("GenerateNewKey", true)});
        passwordAndPrivateKeyViaProxyRules.AddRule("Passphrase", Requirement.Optional);
        passwordAndPrivateKeyViaProxyRules.AddRule("GenerateNewKey", Requirement.Optional);
        _validationRules.Add(ClusterAuthenticationCredentialsAuthType.PasswordAndPrivateKeyViaProxy, passwordAndPrivateKeyViaProxyRules);

        var privateKeyViaProxyRules = new RulesDefinition();
        privateKeyViaProxyRules.AddRule("Password", Requirement.Forbidden);
        privateKeyViaProxyRules.AddRule("ProvidedPrivateKey", Requirement.RequiredConditional, new(){new Conditional("GenerateNewKey", true)});
        privateKeyViaProxyRules.AddRule("Passphrase", Requirement.Optional);
        privateKeyViaProxyRules.AddRule("GenerateNewKey", Requirement.Optional);
        _validationRules.Add(ClusterAuthenticationCredentialsAuthType.PrivateKeyViaProxy, privateKeyViaProxyRules);

        var privateKeyInSshAgentRules = new RulesDefinition();
        privateKeyInSshAgentRules.AddRule("Password", Requirement.Forbidden);
        privateKeyInSshAgentRules.AddRule("ProvidedPrivateKey", Requirement.Forbidden);
        privateKeyInSshAgentRules.AddRule("Passphrase", Requirement.Forbidden);
        privateKeyInSshAgentRules.AddRule("GenerateNewKey", Requirement.Forbidden);
        _validationRules.Add(ClusterAuthenticationCredentialsAuthType.PrivateKeyInSshAgent, privateKeyInSshAgentRules);

        //TODO: what about ClusterAuthenticationCredentialsAuthType.PrivateKeyInVaultAndInSshAgent

        var sshCertificateRules = new RulesDefinition();
        sshCertificateRules.AddRule("Password", Requirement.Forbidden);
        sshCertificateRules.AddRule("ProvidedPrivateKey", Requirement.Required);
        sshCertificateRules.AddRule("Passphrase", Requirement.Optional);
        sshCertificateRules.AddRule("GenerateNewKey", Requirement.Forbidden);
        _validationRules.Add(ClusterAuthenticationCredentialsAuthType.SshCertificate, sshCertificateRules);

        var sshCertificateViaProxyRules = new RulesDefinition();
        sshCertificateViaProxyRules.AddRule("Password", Requirement.Forbidden);
        sshCertificateViaProxyRules.AddRule("ProvidedPrivateKey", Requirement.Required);
        sshCertificateViaProxyRules.AddRule("Passphrase", Requirement.Optional);
        sshCertificateViaProxyRules.AddRule("GenerateNewKey", Requirement.Forbidden);
        _validationRules.Add(ClusterAuthenticationCredentialsAuthType.SshCertificateViaProxy, sshCertificateViaProxyRules);

        var kerberosRules = new RulesDefinition();
        kerberosRules.AddRule("Password", Requirement.Forbidden);
        kerberosRules.AddRule("ProvidedPrivateKey", Requirement.Forbidden);
        kerberosRules.AddRule("Passphrase", Requirement.Forbidden);
        kerberosRules.AddRule("GenerateNewKey", Requirement.Forbidden);
        _validationRules.Add(ClusterAuthenticationCredentialsAuthType.Kerberos, kerberosRules);
        #endregion
    }

    public CredentialValidator(object validationObj) : base(validationObj)
    { }

    public override ValidationResult Validate()
    {
        var message = _validationObject switch
        {
            CreateCredentialModel ext => ValidateCreateCredentialModel(ext),
            _ => string.Empty
        };

        return new ValidationResult(string.IsNullOrEmpty(message), message);
    }

    private List<string> GetValidationFields()
    {
        return _validationRules.First().Value.GetFields();
    }

    private Requirement GetRequirement(ClusterAuthenticationCredentialsAuthType authType, string nameOfField)
    {
        return _validationRules[authType].GetRequirement(nameOfField);
    }

    private bool ConditionsHold(ClusterAuthenticationCredentialsAuthType authType, string nameOfField, object obj)
    {
        return _validationRules[authType].ConditionsHold(nameOfField, obj);
    }

    private void ValidateField(object model, ClusterAuthenticationCredentialsAuthType authType, string nameOfField, object value)
    {
        if(GetRequirement(authType, nameOfField) == Requirement.Forbidden)
        {
            if(value != null)
                _messageBuilder.AppendLine($"{nameOfField} is not applicable for {authType} authentication.");
        }
        else if(GetRequirement(authType, nameOfField) == Requirement.Required)
        {
            if(value == null || (value is string && string.IsNullOrEmpty((string)value)))
                _messageBuilder.AppendLine($"{nameOfField} is required for {authType} authentication.");
        }
        else if(GetRequirement(authType, nameOfField) == Requirement.RequiredConditional)
        {
            if(string.IsNullOrEmpty(value as string) && !ConditionsHold(authType, nameOfField, model))
                _messageBuilder.AppendLine($"{nameOfField} conditions do not hold for {authType} authentication.");
        }
    }

    private string ValidateCreateCredentialModel(CreateCredentialModel ext)
    {
        var sessionCodeValidation = new SessionCodeValidator(ext.SessionCode).Validate();
        if (!sessionCodeValidation.IsValid) 
            _messageBuilder.AppendLine(sessionCodeValidation.Message);

        ValidateId(ext.ProjectId, "ProjectId");

        foreach(string field in GetValidationFields())
            ValidateField(ext, ext.AuthType, field, ext.GetType().GetProperty(field).GetValue(ext));

        return _messageBuilder.ToString();
    }
}