using FluentValidation;
using HEAppE.BusinessLogicTier;
using HEAppE.BusinessLogicTier.AuthMiddleware;
using HEAppE.DataAccessTier.Factory.UnitOfWork;
using HEAppE.DataStagingAPI.API.AbstractTypes;
using HEAppE.DataStagingAPI.Validations.AbstractTypes;
using HEAppE.DomainObjects.UserAndLimitationManagement.Enums;
using HEAppE.ExtModels.FileTransfer.Models;
using HEAppE.ExtModels.General.Models;
using HEAppE.RestApiModels.FileTransfer;
using HEAppE.Services.UserOrg;
using HEAppE.ServiceTier.FileTransfer;
using HEAppE.ServiceTier.UserAndLimitationManagement;
using HEAppE.Utils;
using Microsoft.AspNetCore.Mvc;
#pragma warning disable CS8600, CS8602, CS8603, CS8604, CS8625, CS8632
using System;
using SshCaAPI;

namespace HEAppE.DataStagingAPI.API;

public class DataStagingEndpoint : IApiRoute
{
    public void Register(RouteGroupBuilder group)
    {
        group = group.AddEndpointFilter<AuthorizationKeyFilter>()
            .MapGroup("DataStaging")
            .WithTags("DataStaging");

        group.MapPost("GetFileTransferMethod", async ([Validate] GetFileTransferMethodModel model, [FromServices] ILogger<DataStagingEndpoint> logger, [FromServices] ISshCertificateAuthorityService sshCertificateAuthorityService,
                    [FromServices] IHttpContextKeys httpContextKeys, [FromServices] IUserOrgService userOrgService) =>
                {
                    LoggingUtils.AddJobIdToLogThreadContext(model.SubmittedJobInfoId);
                    logger.LogDebug("""Endpoint: "DataStaging" Method: "GetFileTransferMethod" Parameters: "{@model}" """, model);

                    var result = await (new FileTransferService(userOrgService, sshCertificateAuthorityService, httpContextKeys))
                        .TrustfulRequestFileTransfer(model.SubmittedJobInfoId, model.SessionCode);

                    LoggingUtils.RemoveJobIdFromLogThreadContext();
                    return Results.Ok(result);
                }).Produces<FileTransferMethodExt>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status502BadGateway)
            .RequestSizeLimit(98)
            .WithOpenApi(generatedOperation =>
            {
                generatedOperation.Summary = "Obtain data transfer information for job.";
                generatedOperation.Description = "Obtain credentials and information for ensuring job data transfer.";
                return generatedOperation;
            });

        group.MapPost("ProvideCredentials", async ([Validate] ProvideCredentialsModel model, [FromServices] ILogger<DataStagingEndpoint> logger, [FromServices] ISshCertificateAuthorityService sshCertificateAuthorityService,
                    [FromServices] IHttpContextKeys httpContextKeys, [FromServices] IUserOrgService userOrgService) =>
                {
                    logger.LogDebug("""Endpoint: "DataStaging" Method: "ProvideCredentials" Parameters: "{@model}" """, model);
                    var fileTransferService = new FileTransferService(userOrgService, sshCertificateAuthorityService, httpContextKeys);
                    var result = await fileTransferService.ProvideCredentials(model.ProjectId, model.ClusterId);
                    return Results.Ok(result);
                }).Produces<FileTransferMethodExt>()
                .ProducesValidationProblem()
                .ProducesProblem(StatusCodes.Status401Unauthorized)
                .RequestSizeLimit(98)
                .WithOpenApi(generatedOperation =>
                {
                    generatedOperation.Summary = "Provide credentials for data transfer.";
                    generatedOperation.Description = "Provide credentials for data transfer to support uploading files to cluster-project directory.";
                    return generatedOperation;
                });

        group.MapPost("DownloadPartsOfJobFilesFromCluster", ([Validate] DownloadPartsOfJobFilesFromClusterModel model,
                [FromServices] ILogger<DataStagingEndpoint> logger, [FromServices] ISshCertificateAuthorityService sshCertificateAuthorityService,
                [FromServices] IHttpContextKeys httpContextKeys, [FromServices] IUserOrgService userOrgService) =>
            {
                LoggingUtils.AddJobIdToLogThreadContext(model.SubmittedJobInfoId);
                logger.LogDebug("""Endpoint: "DataStaging" Method: "DownloadPartsOfJobFilesFromCluster" Parameters: "{@model}" """, model);

                var result = new FileTransferService(userOrgService, sshCertificateAuthorityService, httpContextKeys)
                    .DownloadPartsOfJobFilesFromCluster(model.SubmittedJobInfoId, model.TaskFileOffsets, model.SessionCode);

                LoggingUtils.RemoveJobIdFromLogThreadContext();
                return Results.Ok(result);
            }).Produces<JobFileContentExt>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .RequestSizeLimit(480)
            .WithOpenApi(generatedOperation =>
            {
                generatedOperation.Summary = "Get specific part of FileType content.";
                generatedOperation.Description = "Get specific part of FileType content.<br>FileType: LogFile - 0, ProgressFile - 1, StandardErrorFile - 2, StandardOutputFile - 3.";
                return generatedOperation;
            });

        group.MapGet("ListChangedFilesForJob", ([FromQuery(Name = "SessionCode")] string? sessionCode,
                [FromQuery(Name = "SubmittedJobInfoId")] long submittedJobInfoId,
                [FromServices] ILogger<DataStagingEndpoint> logger,
                [FromServices] IValidator<AuthorizedSubmittedJobIdModel> validator, [FromServices] ISshCertificateAuthorityService sshCertificateAuthorityService,
                [FromServices] IHttpContextKeys httpContextKeys, [FromServices] IUserOrgService userOrgService) =>
            {
                LoggingUtils.AddJobIdToLogThreadContext(submittedJobInfoId);
                var model = new AuthorizedSubmittedJobIdModel(sessionCode, submittedJobInfoId);
                validator.ValidateAndThrow(model);

                logger.LogDebug("""Endpoint: "DataStaging" Method: "ListChangedFilesForJob" Parameters: "{@model}" """, model);
                var result = new FileTransferService(userOrgService, sshCertificateAuthorityService, httpContextKeys)
                    .ListChangedFilesForJob(submittedJobInfoId, sessionCode);

                LoggingUtils.RemoveJobIdFromLogThreadContext();
                return Results.Ok(result);
            }).Produces<IEnumerable<FileInformationExt>>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .RequestSizeLimit(98)
            .WithOpenApi(generatedOperation =>
            {
                generatedOperation.Summary = "Get all changed files during job execution.";
                generatedOperation.Description = "Get all changed files during job execution with modification timestamp.";
                return generatedOperation;
            });

        group.MapPost("DownloadFileFromCluster",
                ([Validate] DownloadFileFromClusterModel model, [FromServices] ILogger<DataStagingEndpoint> logger, [FromServices] ISshCertificateAuthorityService sshCertificateAuthorityService,
                    [FromServices] IHttpContextKeys httpContextKeys, [FromServices] IUserOrgService userOrgService) =>
                {
                    LoggingUtils.AddJobIdToLogThreadContext(model.SubmittedJobInfoId);
                    logger.LogDebug("""Endpoint: "FileTransfer" Method: "DownloadFileFromCluster" Parameters: "{@model}" """, model);

                    var result = new FileTransferService(userOrgService, sshCertificateAuthorityService, httpContextKeys)
                        .DownloadFileFromCluster(model.SubmittedJobInfoId, model.RelativeFilePath, model.SessionCode);

                    LoggingUtils.RemoveJobIdFromLogThreadContext();
                    return Results.Ok(result);
                }).Produces<string>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .RequestSizeLimit(378);

        group.MapPost("UploadFilesToProjectDir",
                (
                    [FromQuery(Name = "SessionCode")] string? sessionCode,
                    [FromQuery(Name = "ProjectId")] long projectId,
                    [FromQuery(Name = "ClusterId")] long clusterId,
                    [FromForm] IFormFileCollection files,
                    [FromServices] ILogger<DataStagingEndpoint> logger,
                    [FromServices] IValidator<UploadFileToClusterModel> validator,
                    [FromServices] ISshCertificateAuthorityService sshCertificateAuthorityService,
                    [FromServices] IHttpContextKeys httpContextKeys,
                    [FromServices] IUserOrgService userOrgService
                ) =>
                {
                    var model = new UploadFileToClusterModel() { SessionCode = sessionCode };
                    validator.ValidateAndThrow(model);
                    logger.LogDebug("""Endpoint: "FileTransfer" Method: "UploadFileToClusterModel" Parameters: "{@model}" """, model);

                    CheckValidatedUserForSessionCode(sessionCode, projectId, userOrgService, sshCertificateAuthorityService, httpContextKeys, AdaptorUserRoleType.Manager);

                    var tasks = files.Select(file => new FileTransferService(userOrgService, sshCertificateAuthorityService, httpContextKeys)
                        .UploadFileToProjectDir(file.OpenReadStream(), file.FileName, projectId, clusterId, sessionCode)).ToList();
                    
                    Task.WaitAll(tasks.ToArray());
                    return Results.Ok(doExtractFilesUploadResult(files, tasks));
                })
            .Accepts<IFormFileCollection>("multipart/form-data")
            .Produces<ICollection<FileUploadResultExt>>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .DisableRequestTimeout()
            .RequestSizeLimit(2_200_000_000)
            .DisableAntiforgery();

        group.MapPost("UploadJobScriptsToProjectDir",
                (
                    [FromQuery(Name = "SessionCode")] string? sessionCode,
                    [FromQuery(Name = "ProjectId")] long projectId,
                    [FromQuery(Name = "ClusterId")] long clusterId,
                    [FromForm] IFormFileCollection files,
                    [FromServices] ILogger<DataStagingEndpoint> logger,
                    [FromServices] IValidator<UploadJobScriptsToClusterProjectDirModel> validator,
                    [FromServices] ISshCertificateAuthorityService sshCertificateAuthorityService,
                    [FromServices] IHttpContextKeys httpContextKeys,
                    [FromServices] IUserOrgService userOrgService
                ) =>
                {
                    var model = new UploadJobScriptsToClusterProjectDirModel() { SessionCode = sessionCode };
                    validator.ValidateAndThrow(model);
                    logger.LogDebug("""Endpoint: "FileTransfer" Method: "UploadJobScriptsToClusterProjectDir" Parameters: "{@model}" """, model);

                    CheckValidatedUserForSessionCode(sessionCode, projectId, userOrgService, sshCertificateAuthorityService, httpContextKeys, AdaptorUserRoleType.Manager);

                    var tasks = files.Select(file => new FileTransferService(userOrgService, sshCertificateAuthorityService, httpContextKeys)
                        .UploadJobScriptToProjectDir(file.OpenReadStream(), file.FileName, projectId, clusterId, sessionCode)).ToList();

                    Task.WaitAll(tasks.ToArray());
                    return Results.Ok(doExtractJobsUploadResult(files, tasks));
                })
            .Accepts<IFormFileCollection>("multipart/form-data")
            .Produces<ICollection<FileUploadResultExt>>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .DisableRequestTimeout()
            .RequestSizeLimit(2_200_000_000)
            .DisableAntiforgery();
    }

    static List<FileUploadResultExt> doExtractFilesUploadResult(IFormFileCollection files, List<Task<dynamic>> tasks)
    {
        var result = new List<FileUploadResultExt>();
        for (var i = 0; i < tasks.Count; i++)
        {
            var item = new FileUploadResultExt() { FileName = files[i].FileName, Succeeded = false };
            if (tasks[i].Result is Dictionary<string, dynamic> tr)
            {
                item.Succeeded = tr["Succeeded"];
                item.Path = tr["Path"];
            }
            result.Add(item);
        }
        return result;
    }

    static List<JobUploadResultExt> doExtractJobsUploadResult(IFormFileCollection files, List<Task<dynamic>> tasks)
    {
        var result = new List<JobUploadResultExt>();
        for (var i = 0; i < tasks.Count; i++)
        {
            var item = new JobUploadResultExt() { FileName = files[i].FileName, Succeeded = false };
            if (tasks[i].Result is Dictionary<string, dynamic> tr)
            {
                item.Succeeded = tr["Succeeded"];
                item.Path = tr["Path"];
                if (tr.TryGetValue("AttributesSet", out dynamic? val)) item.AttributesSet = val;
            }
            result.Add(item);
        }
        return result;
    }

    static void CheckValidatedUserForSessionCode(string sessionCode, long projectId, IUserOrgService userOrgService, ISshCertificateAuthorityService sshCertificateAuthorityService, IHttpContextKeys httpContextKeys, AdaptorUserRoleType requiredUserRole)
    {
        using var unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork();
        UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork, userOrgService, sshCertificateAuthorityService, httpContextKeys, requiredUserRole, projectId);
    }
}