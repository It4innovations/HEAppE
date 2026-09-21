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
        { 
            _rules = []; 
            _conditionals = new ();
        }

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
                res &= object.Equals(conditional.Value, obj.GetType().GetProperty(conditional.Field).GetValue(obj));
            }
            return res;
        }
    }

    private const string PASSWORD = "Password";
    private const string PROVIDED_PRIVATE_KEY = "ProvidedPrivateKey";
    private const string PASSPHRASE = "Passphrase";
    private const string GENERATE_NEW_KEY = "GenerateNewKey";
    static readonly Dictionary<ClusterAuthenticationCredentialsAuthType, RulesDefinition> _validationCreateCredentialRules;

    static CredentialValidator()
    {
        #region Rules assignment
        _validationCreateCredentialRules = new();

        var passwordRules = new RulesDefinition();
        passwordRules.AddRule(PASSWORD, Requirement.Required);
        passwordRules.AddRule(PROVIDED_PRIVATE_KEY, Requirement.Forbidden);
        passwordRules.AddRule(PASSPHRASE, Requirement.Forbidden);
        passwordRules.AddRule(GENERATE_NEW_KEY, Requirement.Forbidden);
        _validationCreateCredentialRules.Add(ClusterAuthenticationCredentialsAuthType.Password, passwordRules);

        var passwordInteractiveRules = new RulesDefinition();
        passwordInteractiveRules.AddRule(PASSWORD, Requirement.Optional);
        passwordInteractiveRules.AddRule(PROVIDED_PRIVATE_KEY, Requirement.Forbidden);
        passwordInteractiveRules.AddRule(PASSPHRASE, Requirement.Forbidden);
        passwordInteractiveRules.AddRule(GENERATE_NEW_KEY, Requirement.Forbidden);
        _validationCreateCredentialRules.Add(ClusterAuthenticationCredentialsAuthType.PasswordInteractive, passwordInteractiveRules);

        var passwordAndPrivateKeyRules = new RulesDefinition();
        passwordAndPrivateKeyRules.AddRule(PASSWORD, Requirement.Required);
        passwordAndPrivateKeyRules.AddRule(PROVIDED_PRIVATE_KEY, Requirement.RequiredConditional, new(){new Conditional(GENERATE_NEW_KEY, true)});
        passwordAndPrivateKeyRules.AddRule(PASSPHRASE, Requirement.Optional);
        passwordAndPrivateKeyRules.AddRule(GENERATE_NEW_KEY, Requirement.Optional);
        _validationCreateCredentialRules.Add(ClusterAuthenticationCredentialsAuthType.PasswordAndPrivateKey, passwordAndPrivateKeyRules);

        var privateKeyRules = new RulesDefinition();
        privateKeyRules.AddRule(PASSWORD, Requirement.Forbidden);
        privateKeyRules.AddRule(PROVIDED_PRIVATE_KEY, Requirement.RequiredConditional, new(){new Conditional(GENERATE_NEW_KEY, true)});
        privateKeyRules.AddRule(PASSPHRASE, Requirement.Optional);
        privateKeyRules.AddRule(GENERATE_NEW_KEY, Requirement.Optional);
        _validationCreateCredentialRules.Add(ClusterAuthenticationCredentialsAuthType.PrivateKey, privateKeyRules);

        var passwordViaProxyRules = new RulesDefinition();
        passwordViaProxyRules.AddRule(PASSWORD, Requirement.Required);
        passwordViaProxyRules.AddRule(PROVIDED_PRIVATE_KEY, Requirement.Forbidden);
        passwordViaProxyRules.AddRule(PASSPHRASE, Requirement.Forbidden);
        passwordViaProxyRules.AddRule(GENERATE_NEW_KEY, Requirement.Forbidden);
        _validationCreateCredentialRules.Add(ClusterAuthenticationCredentialsAuthType.PasswordViaProxy, passwordViaProxyRules);

        var passwordInteractiveViaProxyRules = new RulesDefinition();
        passwordInteractiveViaProxyRules.AddRule(PASSWORD, Requirement.Optional);
        passwordInteractiveViaProxyRules.AddRule(PROVIDED_PRIVATE_KEY, Requirement.Forbidden);
        passwordInteractiveViaProxyRules.AddRule(PASSPHRASE, Requirement.Forbidden);
        passwordInteractiveViaProxyRules.AddRule(GENERATE_NEW_KEY, Requirement.Forbidden);
        _validationCreateCredentialRules.Add(ClusterAuthenticationCredentialsAuthType.PasswordInteractiveViaProxy, passwordInteractiveViaProxyRules);

        var passwordAndPrivateKeyViaProxyRules = new RulesDefinition();
        passwordAndPrivateKeyViaProxyRules.AddRule(PASSWORD, Requirement.Required);
        passwordAndPrivateKeyViaProxyRules.AddRule(PROVIDED_PRIVATE_KEY, Requirement.RequiredConditional, new(){new Conditional(GENERATE_NEW_KEY, true)});
        passwordAndPrivateKeyViaProxyRules.AddRule(PASSPHRASE, Requirement.Optional);
        passwordAndPrivateKeyViaProxyRules.AddRule(GENERATE_NEW_KEY, Requirement.Optional);
        _validationCreateCredentialRules.Add(ClusterAuthenticationCredentialsAuthType.PasswordAndPrivateKeyViaProxy, passwordAndPrivateKeyViaProxyRules);

        var privateKeyViaProxyRules = new RulesDefinition();
        privateKeyViaProxyRules.AddRule(PASSWORD, Requirement.Forbidden);
        privateKeyViaProxyRules.AddRule(PROVIDED_PRIVATE_KEY, Requirement.RequiredConditional, new(){new Conditional(GENERATE_NEW_KEY, true)});
        privateKeyViaProxyRules.AddRule(PASSPHRASE, Requirement.Optional);
        privateKeyViaProxyRules.AddRule(GENERATE_NEW_KEY, Requirement.Optional);
        _validationCreateCredentialRules.Add(ClusterAuthenticationCredentialsAuthType.PrivateKeyViaProxy, privateKeyViaProxyRules);

        var privateKeyInSshAgentRules = new RulesDefinition();
        privateKeyInSshAgentRules.AddRule(PASSWORD, Requirement.Forbidden);
        privateKeyInSshAgentRules.AddRule(PROVIDED_PRIVATE_KEY, Requirement.Forbidden);
        privateKeyInSshAgentRules.AddRule(PASSPHRASE, Requirement.Forbidden);
        privateKeyInSshAgentRules.AddRule(GENERATE_NEW_KEY, Requirement.Forbidden);
        _validationCreateCredentialRules.Add(ClusterAuthenticationCredentialsAuthType.PrivateKeyInSshAgent, privateKeyInSshAgentRules);

        //TODO: what about ClusterAuthenticationCredentialsAuthType.PrivateKeyInVaultAndInSshAgent

        var sshCertificateRules = new RulesDefinition();
        sshCertificateRules.AddRule(PASSWORD, Requirement.Forbidden);
        sshCertificateRules.AddRule(PROVIDED_PRIVATE_KEY, Requirement.RequiredConditional, new(){new Conditional(GENERATE_NEW_KEY, true)});
        sshCertificateRules.AddRule(PASSPHRASE, Requirement.Optional);
        sshCertificateRules.AddRule(GENERATE_NEW_KEY, Requirement.Optional);
        _validationCreateCredentialRules.Add(ClusterAuthenticationCredentialsAuthType.SshCertificate, sshCertificateRules);

        var sshCertificateViaProxyRules = new RulesDefinition();
        sshCertificateViaProxyRules.AddRule(PASSWORD, Requirement.Forbidden);
        sshCertificateViaProxyRules.AddRule(PROVIDED_PRIVATE_KEY, Requirement.RequiredConditional, new(){new Conditional(GENERATE_NEW_KEY, true)});
        sshCertificateViaProxyRules.AddRule(PASSPHRASE, Requirement.Optional);
        sshCertificateViaProxyRules.AddRule(GENERATE_NEW_KEY, Requirement.Optional);
        _validationCreateCredentialRules.Add(ClusterAuthenticationCredentialsAuthType.SshCertificateViaProxy, sshCertificateViaProxyRules);

        var kerberosRules = new RulesDefinition();
        kerberosRules.AddRule(PASSWORD, Requirement.Forbidden);
        kerberosRules.AddRule(PROVIDED_PRIVATE_KEY, Requirement.Forbidden);
        kerberosRules.AddRule(PASSPHRASE, Requirement.Forbidden);
        kerberosRules.AddRule(GENERATE_NEW_KEY, Requirement.Forbidden);
        _validationCreateCredentialRules.Add(ClusterAuthenticationCredentialsAuthType.Kerberos, kerberosRules);

        #endregion
    }

    public CredentialValidator(object validationObj) : base(validationObj)
    { }

    public override ValidationResult Validate()
    {
        var message = _validationObject switch
        {
            CreateCredentialModel ext => ValidateCreateCredentialModel(ext),
            GetCredentialsModel ext => ValidateGetCredentialsModel(ext),
            ModifyCredentialModel ext => ValidateModifyCredentialModel(ext),
            RemoveCredentialModel ext => ValidateRemoveCredentialModel(ext),
            _ => string.Empty
        };

        return new ValidationResult(string.IsNullOrEmpty(message), message);
    }

    private List<string> GetValidationCreateCredentialFields()
    {
        return _validationCreateCredentialRules.First().Value.GetFields();
    }

    private Requirement GetRequirement(ClusterAuthenticationCredentialsAuthType authType, string nameOfField)
    {
        return _validationCreateCredentialRules[authType].GetRequirement(nameOfField);
    }

    private bool ConditionsHold(ClusterAuthenticationCredentialsAuthType authType, string nameOfField, object obj)
    {
        return _validationCreateCredentialRules[authType].ConditionsHold(nameOfField, obj);
    }

    private void ValidateCreateCredentialField(object model, ClusterAuthenticationCredentialsAuthType authType, string nameOfField, object value)
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
        if (ext.AdaptorUserId.HasValue)
            ValidateId(ext.AdaptorUserId.Value, "AdaptorUserId");

        if (ext.AuthType.HasValue)
        {
            foreach (string field in GetValidationCreateCredentialFields())
                ValidateCreateCredentialField(ext, ext.AuthType.Value, field, ext.GetType().GetProperty(field).GetValue(ext));
        }

        return _messageBuilder.ToString();
    }

    private string ValidateGetCredentialsModel(GetCredentialsModel ext)
    {
        var sessionCodeValidation = new SessionCodeValidator(ext.SessionCode).Validate();
        if (!sessionCodeValidation.IsValid) 
            _messageBuilder.AppendLine(sessionCodeValidation.Message);

        ValidateId(ext.ProjectId, "ProjectId");
        if (ext.AdaptorUserId.HasValue)
            ValidateId(ext.AdaptorUserId.Value, "AdaptorUserId");

        return _messageBuilder.ToString();
    }

    private string ValidateModifyCredentialModel(ModifyCredentialModel ext)
    {
        var sessionCodeValidation = new SessionCodeValidator(ext.SessionCode).Validate();
        if (!sessionCodeValidation.IsValid) 
            _messageBuilder.AppendLine(sessionCodeValidation.Message);

        ValidateId(ext.ProjectId, "ProjectId");
        if (ext.AdaptorUserId.HasValue)
            ValidateId(ext.AdaptorUserId.Value, "AdaptorUserId");

        if (string.IsNullOrEmpty(ext.OldUsername)) 
            _messageBuilder.AppendLine("OldUsername can not be null or empty.");
        if (string.IsNullOrEmpty(ext.NewUsername)) 
            _messageBuilder.AppendLine("NewUsername can not be null or empty.");

        return _messageBuilder.ToString();
    }

    private string ValidateRemoveCredentialModel(RemoveCredentialModel ext)
    {
        var sessionCodeValidation = new SessionCodeValidator(ext.SessionCode).Validate();
        if (!sessionCodeValidation.IsValid) 
            _messageBuilder.AppendLine(sessionCodeValidation.Message);

        ValidateId(ext.ProjectId, "ProjectId");
        if (ext.AdaptorUserId.HasValue)
            ValidateId(ext.AdaptorUserId.Value, "AdaptorUserId");

        return _messageBuilder.ToString();
    }
}