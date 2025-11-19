using FluentValidation;
using HEAppE.BusinessLogicTier;
using HEAppE.DataStagingAPI.API.AbstractTypes;
using HEAppE.DataStagingAPI.Validations.AbstractTypes;
using HEAppE.ExtModels.FileTransfer.Models;
using HEAppE.ExtModels.General.Models;
using HEAppE.RestApiModels.FileTransfer;
using HEAppE.ServiceTier.FileTransfer;
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


        group.MapPost("GetFileTransferMethod",
                ([Validate] GetFileTransferMethodModel model, [FromServices] ILogger<DataStagingEndpoint> logger, [FromServices] ISshCertificateAuthorityService sshCertificateAuthorityService,
                    [FromServices] IHttpContextKeys httpContextKeys) =>
                {
                    logger.LogDebug(
                        """Endpoint: "DataStaging" Method: "GetFileTransferMethod" Parameters: "{@model}" """, model);
                    return Results.Ok(
                        new FileTransferService(sshCertificateAuthorityService, httpContextKeys).TrustfulRequestFileTransfer(model.SubmittedJobInfoId,
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
                [FromServices] IHttpContextKeys httpContextKeys) =>
            {
                logger.LogDebug(
                    """Endpoint: "DataStaging" Method: "DownloadPartsOfJobFilesFromCluster" Parameters: "{@model}" """,
                    model);
                return Results.Ok(new FileTransferService(sshCertificateAuthorityService, httpContextKeys).DownloadPartsOfJobFilesFromCluster(model.SubmittedJobInfoId,
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
                [FromServices] IHttpContextKeys httpContextKeys) =>
            {
                var model = new AuthorizedSubmittedJobIdModel(sessionCode, submittedJobInfoId);
                validator.ValidateAndThrow(model);

                logger.LogDebug("""Endpoint: "DataStaging" Method: "ListChangedFilesForJob" Parameters: "{@model}" """,
                    model);
                return Results.Ok(new FileTransferService(sshCertificateAuthorityService, httpContextKeys).ListChangedFilesForJob(submittedJobInfoId, sessionCode));
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
                    [FromServices] IHttpContextKeys httpContextKeys) =>
                {
                    logger.LogDebug(
                        """Endpoint: "FileTransfer" Method: "DownloadFileFromCluster" Parameters: "{@model}" """,
                        model);
                    return Results.Ok(new FileTransferService(sshCertificateAuthorityService, httpContextKeys).DownloadFileFromCluster(model.SubmittedJobInfoId,
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
    }
}