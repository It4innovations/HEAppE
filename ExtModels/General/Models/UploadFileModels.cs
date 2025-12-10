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
[DataContract(Name = "UploadFileToProjectStorageDir")]
[Description("Model for upload file to cluster")]
public class UploadFileToProjectStorageDirModel
{

    /// <summary>
    /// Session code
    /// </summary>
    [Description("Session code")]
    public string SessionCode { get; set; }
    public override string ToString()
    {
        return $"UploadFileToProjectStorageDirModel({base.ToString()};)";
    }
}

/// <summary>
/// Model for upload file to cluster
/// </summary>
[DataContract(Name = "UploadJobScriptsToClusterProjectDir")]
[Description("Model for upload file to cluster")]
public class UploadJobScriptsToClusterProjectDirModel
{

    /// <summary>
    /// Session code
    /// </summary>
    [Description("Session code")]
    public string SessionCode { get; set; }
    public override string ToString()
    {
        return $"UploadJobScriptsToClusterProjectDirModel(SessionCode={SessionCode};)";
    }
}

public class UploadFileToProjectStorageDirModelValidator : AbstractValidator<UploadJobScriptsToClusterProjectDirModel>
{
    public UploadFileToProjectStorageDirModelValidator()
    {
        if (!JwtTokenIntrospectionConfiguration.LexisTokenFlowConfiguration.IsEnabled &&
            !LexisAuthenticationConfiguration.UseBearerAuth)
        {
            RuleFor(x => x.SessionCode).IsSessionCode();
        }
    }
}