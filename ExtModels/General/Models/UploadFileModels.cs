using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using FluentValidation;

namespace HEAppE.ExtModels.General.Models;

/// <summary>
/// Model for upload file to cluster
/// </summary>
[DataContract(Name = "UploadFileToPermanentStorageDirModel")]
[Description("Model for upload file to cluster")]
public class UploadFileToPermanentStorageDirModel
{

    /// <summary>
    /// Session code
    /// </summary>
    [Description("Session code")]
    public string SessionCode { get; set; }
    public override string ToString()
    {
        return $"UploadFileToClusterModel({base.ToString()};)";
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

public class UploadFileToPermanentStorageDirModelValidator : AbstractValidator<UploadJobScriptsToClusterProjectDirModel>
{
    public UploadFileToPermanentStorageDirModelValidator()
    {
        RuleFor(x => x.SessionCode).IsSessionCode();
    }
}