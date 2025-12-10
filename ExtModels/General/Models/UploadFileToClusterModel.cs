using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using FluentValidation;
using HEAppE.ExternalAuthentication.Configuration;

namespace HEAppE.ExtModels.General.Models;

/// <summary>
/// Model for upload file to cluster
/// </summary>
[DataContract(Name = "UploadFileToClusterModel")]
[Description("Model for upload file to cluster")]
public class UploadFileToClusterModel
{

    /// <summary>
    /// Session code
    /// </summary>
    [Description("Session code")]
    public string SessionCode { get; set; }

    /// <summary>
    /// Relative file path on cluster
    /// </summary>
    [DataMember(Name = "RelativeFilePath")]
    [StringLength(255)]
    [Description("Relative file path on cluster")]
    public string RelativeFilePath { get; set; }

    public override string ToString()
    {
        return $"UploadFileToClusterModel({base.ToString()}; RelativeFilePath: {RelativeFilePath})";
    }
}

public class UploadFileToClusterModelValidator : AbstractValidator<UploadFileToClusterModel>
{
    public UploadFileToClusterModelValidator()
    {
        if (!JwtTokenIntrospectionConfiguration.LexisTokenFlowConfiguration.IsEnabled &&
            !LexisAuthenticationConfiguration.UseBearerAuth)
        {
            RuleFor(x => x.SessionCode).IsSessionCode();
        }

    }
}