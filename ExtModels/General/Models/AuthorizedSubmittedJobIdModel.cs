using FluentValidation;
using System.ComponentModel;
using HEAppE.ExternalAuthentication.Configuration;

namespace HEAppE.ExtModels.General.Models;

/// <summary>
/// Authorized submitted job id model
/// </summary>
[Description("Authorized submitted job id model")]
public class AuthorizedSubmittedJobIdModel
{
    #region Properties

    /// <summary>
    /// Session code
    /// </summary>
    [Description("Session code")] 
    public string SessionCode { get; set; }

    /// <summary>
    /// Submitted job info id
    /// </summary>
    [Description("Submitted job info id")] 
    public long SubmittedJobInfoId { get; set; }

    #endregion

    #region Constructors

    public AuthorizedSubmittedJobIdModel()
    {
    }

    public AuthorizedSubmittedJobIdModel(string sessionCode, long submittedJobInfoId)
    {
        SessionCode = sessionCode;
        SubmittedJobInfoId = submittedJobInfoId;
    }

    #endregion
}

public class AuthorizedSubmittedJobIdModelValidator : AbstractValidator<AuthorizedSubmittedJobIdModel>
{
    public AuthorizedSubmittedJobIdModelValidator()
    {
        if (!JwtTokenIntrospectionConfiguration.LexisTokenFlowConfiguration.IsEnabled && !LexisAuthenticationConfiguration.UseBearerAuth)
        {
            RuleFor(x => x.SessionCode).IsSessionCode();
        }
        RuleFor(x => x.SubmittedJobInfoId).NotEmpty().GreaterThan(0);
    }
}