using FluentValidation;
using HEAppE.BusinessLogicTier;
using HEAppE.DataAccessTier.Factory.UnitOfWork;
using HEAppE.DataAccessTier.UnitOfWork;
using HEAppE.DataStagingAPI.API.AbstractTypes;
using HEAppE.DataStagingAPI.Validations.AbstractTypes;
using HEAppE.DomainObjects.UserAndLimitationManagement.Enums;
using HEAppE.ExtModels.FileTransfer.Models;
using HEAppE.ExtModels.General.Models;
using HEAppE.RestApiModels.AbstractModels;
using HEAppE.RestApiModels.FileTransfer;
using HEAppE.ServiceTier.FileTransfer;
using HEAppE.ServiceTier.UserAndLimitationManagement;
using Microsoft.AspNetCore.Mvc;
using SshCaAPI;

namespace HEAppE.DataStagingAPI.API;

/// <summary>
///     Data Stagging Endpoint
/// </summary>
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
                    logger.LogDebug(
                        """Endpoint: "DataStaging" Method: "GetFileTransferMethod" Parameters: "{@model}" """, model);
                    return Results.Ok( await
                        (new FileTransferService(userOrgService, sshCertificateAuthorityService, httpContextKeys)).TrustfulRequestFileTransfer(model.SubmittedJobInfoId,
                            model.SessionCode));
                }).Produces<FileTransferMethodExt>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status413PayloadTooLarge)
            .ProducesProblem(StatusCodes.Status429TooManyRequests)
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .RequestSizeLimit(98)
            .WithOpenApi(generatedOperation =>
            {
                generatedOperation.Summary = "Obtain data transfer information for job.";
                generatedOperation.Description = "Obtain credentials and information for ensuring job data transfer.";
                return generatedOperation;
            });


        group.MapPost("DownloadPartsOfJobFilesFromCluster", ([Validate] DownloadPartsOfJobFilesFromClusterModel model,
                [FromServices] ILogger<DataStagingEndpoint> logger, [FromServices] ISshCertificateAuthorityService sshCertificateAuthorityService,
                [FromServices] IHttpContextKeys httpContextKeys, [FromServices] IUserOrgService userOrgService) =>
            {
                logger.LogDebug(
                    """Endpoint: "DataStaging" Method: "DownloadPartsOfJobFilesFromCluster" Parameters: "{@model}" """,
                    model);
                return Results.Ok(new FileTransferService(userOrgService, sshCertificateAuthorityService, httpContextKeys).DownloadPartsOfJobFilesFromCluster(model.SubmittedJobInfoId,
                    model.TaskFileOffsets, model.SessionCode));
            }).Produces<JobFileContentExt>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status413PayloadTooLarge)
            .ProducesProblem(StatusCodes.Status429TooManyRequests)
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .RequestSizeLimit(480)
            .WithOpenApi(generatedOperation =>
            {
                generatedOperation.Summary = "Get specific part of FileType content.";
                generatedOperation.Description =
                    "Get specific part of FileType content.<br>FileType: LogFile - 0, ProgressFile - 1, StandardErrorFile - 2, StandardOutputFile - 3.";
                return generatedOperation;
            });


        group.MapGet("ListChangedFilesForJob", ([FromQuery(Name = "SessionCode")] string? sessionCode,
                [FromQuery(Name = "SubmittedJobInfoId")] long submittedJobInfoId,
                [FromServices] ILogger<DataStagingEndpoint> logger,
                [FromServices] IValidator<AuthorizedSubmittedJobIdModel> validator, [FromServices] ISshCertificateAuthorityService sshCertificateAuthorityService,
                [FromServices] IHttpContextKeys httpContextKeys, [FromServices] IUserOrgService userOrgService) =>
            {
                var model = new AuthorizedSubmittedJobIdModel(sessionCode, submittedJobInfoId);
                validator.ValidateAndThrow(model);

                logger.LogDebug("""Endpoint: "DataStaging" Method: "ListChangedFilesForJob" Parameters: "{@model}" """,
                    model);
                return Results.Ok(new FileTransferService(userOrgService, sshCertificateAuthorityService, httpContextKeys).ListChangedFilesForJob(submittedJobInfoId, sessionCode));
            }).Produces<IEnumerable<FileInformationExt>>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status413PayloadTooLarge)
            .ProducesProblem(StatusCodes.Status429TooManyRequests)
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .RequestSizeLimit(98)
            .WithOpenApi(generatedOperation =>
            {
                generatedOperation.Summary = "Get all changed files during job execution.";
                generatedOperation.Description =
                    "Get all changed files during job execution with modification timestamp.";
                var parameter = generatedOperation.Parameters[0];
                parameter.Description = "SessionCode";
                var parameter2 = generatedOperation.Parameters[1];
                parameter2.Description = "SubmittedJobInfoId";
                return generatedOperation;
            });

        group.MapPost("DownloadFileFromCluster",
                ([Validate] DownloadFileFromClusterModel model, [FromServices] ILogger<DataStagingEndpoint> logger, [FromServices] ISshCertificateAuthorityService sshCertificateAuthorityService,
                    [FromServices] IHttpContextKeys httpContextKeys, [FromServices] IUserOrgService userOrgService) =>
                {
                    logger.LogDebug(
                        """Endpoint: "FileTransfer" Method: "DownloadFileFromCluster" Parameters: "{@model}" """,
                        model);
                    return Results.Ok(new FileTransferService(userOrgService, sshCertificateAuthorityService, httpContextKeys).DownloadFileFromCluster(model.SubmittedJobInfoId,
                        model.RelativeFilePath, model.SessionCode));
                }).Produces<string>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status413PayloadTooLarge)
            .ProducesProblem(StatusCodes.Status429TooManyRequests)
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .RequestSizeLimit(378)
            .WithOpenApi(generatedOperation =>
            {
                generatedOperation.Summary = "Get content of file";
                generatedOperation.Description =
                    "Get content of the specific file on HPC infrastructure. Content is encoded in BASE64 format.";
                return generatedOperation;
            });

        static List<FileUploadResultExt> doExtractFilesUploadResult(IFormFileCollection files, List<Task<dynamic>> tasks)
        {
            var result = new List<FileUploadResultExt>();
            for (var i = 0; i < tasks.Count; i++)
            {
                var task = tasks[i];
                var file = files[i];
                var item = new FileUploadResultExt() { FileName = file.FileName, Succeeded = false, Path = null };
                result.Add(item);

                Dictionary<string, dynamic> taskResult = task.Result;
                if (taskResult == null)
                    continue;
                item.Succeeded = taskResult["Succeeded"];
                item.Path = taskResult["Path"];
            }
            return result;
        }

        static List<JobUploadResultExt> doExtractJobsUploadResult(IFormFileCollection files, List<Task<dynamic>> tasks)
        {
            var result = new List<JobUploadResultExt>();
            for (var i = 0; i < tasks.Count; i++)
            {
                var task = tasks[i];
                var file = files[i];
                var item = new JobUploadResultExt() { FileName = file.FileName, Succeeded = false, Path = null, AttributesSet = null };
                result.Add(item);

                Dictionary<string, dynamic> taskResult = task.Result;
                if (taskResult == null)
                    continue;
                item.Succeeded = taskResult["Succeeded"];
                item.Path = taskResult["Path"];
                if (taskResult.TryGetValue("AttributesSet", out dynamic? value))
                    item.AttributesSet = value;
            }
            return result;
        }

        static void CheckValidatedUserForSessionCode(string sessionCode, long projectId, IUserOrgService userOrgService, ISshCertificateAuthorityService sshCertificateAuthorityService, IHttpContextKeys httpContextKeys, AdaptorUserRoleType requiredUserRole)
        {
            using (var unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
            {
                var loggedUser = UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork, userOrgService, sshCertificateAuthorityService, httpContextKeys,
                    requiredUserRole, projectId);
            }
        }

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
                    logger.LogDebug(
                        """Endpoint: "FileTransfer" Method: "UploadFileToClusterModel" Parameters: "{@model}" """,
                        model);

                    CheckValidatedUserForSessionCode(sessionCode, projectId, userOrgService, sshCertificateAuthorityService, httpContextKeys, AdaptorUserRoleType.Manager);

                    var tasks = new List<Task<dynamic>>();
                    foreach (var file in files)
                    {
                        tasks.Add(new FileTransferService(userOrgService, sshCertificateAuthorityService, httpContextKeys).UploadFileToProjectDir(file.OpenReadStream(), file.FileName, projectId, clusterId, sessionCode));
                    }
                    Task.WaitAll(tasks);

                    List<FileUploadResultExt> result = doExtractFilesUploadResult(files, tasks);
                    return Results.Ok(result);


                })
            .Accepts<IFormFileCollection>("multipart/form-data")
            .Produces<ICollection<FileUploadResultExt>>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status413PayloadTooLarge)
            .ProducesProblem(StatusCodes.Status429TooManyRequests)
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .DisableRequestTimeout()
            .RequestSizeLimit(2_200_000_000)
            .DisableAntiforgery()
            .WithOpenApi(generatedOperation =>
            {
                generatedOperation.Summary = "Upload multiple files.";
                generatedOperation.Description =
                    "Upload multiple files to project storage directory.";
                return generatedOperation;
            });

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
                    logger.LogDebug(
                        """Endpoint: "FileTransfer" Method: "UploadJobScriptsToClusterProjectDir" Parameters: "{@model}" """,
                        model);

                    CheckValidatedUserForSessionCode(sessionCode, projectId, userOrgService, sshCertificateAuthorityService, httpContextKeys, AdaptorUserRoleType.Manager);

                    var tasks = new List<Task<dynamic>>();
                    foreach (var file in files)
                    {
                        tasks.Add(new FileTransferService(userOrgService, sshCertificateAuthorityService, httpContextKeys).UploadJobScriptToProjectDir(file.OpenReadStream(), file.FileName, projectId, clusterId, sessionCode));
                    }
                    Task.WaitAll(tasks);

                    List<JobUploadResultExt> result = doExtractJobsUploadResult(files, tasks);
                    return Results.Ok(result);
                })
            .Accepts<IFormFileCollection>("multipart/form-data")
            .Produces<ICollection<FileUploadResultExt>>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status413PayloadTooLarge)
            .ProducesProblem(StatusCodes.Status429TooManyRequests)
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .DisableRequestTimeout()
            .RequestSizeLimit(2_200_000_000)
            .DisableAntiforgery()
            .WithOpenApi(generatedOperation =>
            {
                generatedOperation.Summary = "Upload multiple files.";
                generatedOperation.Description =
                    "Upload multiple job scripts to cluster-project directory.";
                return generatedOperation;
            });

    }
}