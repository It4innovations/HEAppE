using HEAppE.DataAccessTier.Factory.UnitOfWork;
using HEAppE.DataAccessTier.UnitOfWork;
using HEAppE.DomainObjects.JobManagement;
using HEAppE.DomainObjects.JobReporting.Enums;
using HEAppE.Exceptions.External;
using HEAppE.ExtModels.ClusterInformation.Models;
using HEAppE.ExtModels.JobManagement.Converts;
using HEAppE.ExtModels.JobManagement.Models;
using HEAppE.ExtModels.Management.Converts;
using HEAppE.ExtModels.Management.Models;
using HEAppE.RestApi.Configuration;
using HEAppE.RestApi.InputValidator;
using HEAppE.RestApiModels.Management;
using HEAppE.ServiceTier.Management;
using HEAppE.ServiceTier.UserAndLimitationManagement;
using HEAppE.Utils;
using HEAppE.Utils.Validation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using HEAppE.DomainObjects.UserAndLimitationManagement;

namespace HEAppE.RestApi.Controllers
{
    [ApiController]
    [Route("heappe/[controller]")]
    [Produces("application/json")]
    public class ManagementController : BaseController<ManagementController>
    {
        #region Instances
        private readonly IManagementService _managementService;
        private readonly IUserAndLimitationManagementService _userAndManagementService;
        #endregion
        #region Constructors
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="logger">Logger instance</param>
        /// <param name="memoryCache">Memory cache provider</param>
        public ManagementController(ILogger<ManagementController> logger, IMemoryCache memoryCache) : base(logger, memoryCache)
        {
            _managementService = new ManagementService();
            _userAndManagementService = new UserAndLimitationManagementService(memoryCache);
        }
        #endregion
        #region Methods
        #region InstanceInformation
        /// <summary>
        /// Get HEAppE Information
        /// </summary>
        /// <param name="sessionCode">SessionCode</param>
        /// <returns></returns>
        [HttpGet("InstanceInformation")]
        [RequestSizeLimit(90)]
        [ProducesResponseType(typeof(InstanceInformationExt), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status413RequestEntityTooLarge)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public IActionResult InstanceInformation(string sessionCode)
        {
            _logger.LogDebug($"Endpoint: \"Management\" Method: \"InstanceInformation\" Parameters: SessionCode: \"{sessionCode}\"");
            ValidationResult validationResult = new SessionCodeValidator(sessionCode).Validate();
            if (!validationResult.IsValid)
            {
                throw new InputValidationException(validationResult.Message);
            }

            _userAndManagementService.ValidateUserPermissions(sessionCode, DomainObjects.UserAndLimitationManagement.Enums.AdaptorUserRoleType.Administrator);
            List<ExtendedProjectInfoExt> activeProjectsExtendedInfo = new();
            using (IUnitOfWork unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
            {
                activeProjectsExtendedInfo = unitOfWork.ProjectRepository.GetAllActiveProjects()?.Select(p => p.ConvertIntToExtendedInfoExt()).ToList();
            }

            return Ok(new InstanceInformationExt()
            {
                Name = DeploymentInformationsConfiguration.Name,
                Description = DeploymentInformationsConfiguration.Description,
                Version = DeploymentInformationsConfiguration.Version,
                DeployedIPAddress = DeploymentInformationsConfiguration.DeployedIPAddress,
                Port = DeploymentInformationsConfiguration.Port,
                URL = DeploymentInformationsConfiguration.Host,
                URLPostfix = DeploymentInformationsConfiguration.HostPostfix,
                DeploymentType = DeploymentInformationsConfiguration.DeploymentType.ConvertIntToExt(),
                ResourceAllocationTypes = DeploymentInformationsConfiguration.ResourceAllocationTypes?.Select(s => s.ConvertIntToExt()).ToList(),
                Projects = activeProjectsExtendedInfo
            });
        }

        /// <summary>
        /// Get HEAppE Version Information
        /// </summary>
        /// <param name="sessionCode">SessionCode</param>
        /// <returns></returns>
        [HttpGet("VersionInformation")]
        [RequestSizeLimit(90)]
        [ProducesResponseType(typeof(VersionInformationExt), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status413RequestEntityTooLarge)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public IActionResult VersionInformation(string sessionCode)
        {
            _logger.LogDebug($"Endpoint: \"Management\" Method: \"VersionInformation\" Parameters: SessionCode: \"{sessionCode}\"");
            ValidationResult validationResult = new SessionCodeValidator(sessionCode).Validate();
            if (!validationResult.IsValid)
            {
                throw new InputValidationException(validationResult.Message);
            }

            _userAndManagementService.ValidateUserPermissions(sessionCode, DomainObjects.UserAndLimitationManagement.Enums.AdaptorUserRoleType.Submitter);
            return Ok(new VersionInformationExt()
            {
                Name = DeploymentInformationsConfiguration.Name,
                Description = DeploymentInformationsConfiguration.Description,
                Version = DeploymentInformationsConfiguration.Version,
            });
        }

        #endregion
        #region CommandTemplate
        /// <summary>
        /// List Command Template
        /// </summary>
        /// <param name="sessionCode"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        /// <exception cref="InputValidationException"></exception>
        [HttpGet("CommandTemplate")]
        [RequestSizeLimit(100)]
        [ProducesResponseType(typeof(ExtendedCommandTemplateExt), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status413RequestEntityTooLarge)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public IActionResult ListCommandTemplate(string sessionCode, long id)
        {
            _logger.LogDebug($"Endpoint: \"Management\" Method: \"ListCommandTemplate\"");
            var model = new ListCommandTemplateModel()
            {
                SessionCode = sessionCode,
                Id = id
            };
            ValidationResult validationResult = new ManagementValidator(model).Validate();
            if (!validationResult.IsValid)
            {
                throw new InputValidationException(validationResult.Message);
            }
            
            return Ok(_managementService.ListCommandTemplate(id, sessionCode));
        }
        
        /// <summary>
        /// List Command Templates
        /// </summary>
        /// <param name="sessionCode"></param>
        /// <param name="projectId"></param>
        /// <returns></returns>
        /// <exception cref="InputValidationException"></exception>
        [HttpGet("CommandTemplates")]
        [RequestSizeLimit(100)]
        [ProducesResponseType(typeof(List<ExtendedCommandTemplateExt>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status413RequestEntityTooLarge)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public IActionResult ListCommandTemplates(string sessionCode, long projectId)
        {
            _logger.LogDebug($"Endpoint: \"Management\" Method: \"ListCommandTemplates\"");
            var listCommandTemplatesModel = new ListCommandTemplatesModel()
            {
                SessionCode = sessionCode,
                ProjectId = projectId
            };
            ValidationResult validationResult = new ManagementValidator(listCommandTemplatesModel).Validate();
            if (!validationResult.IsValid)
            {
                throw new InputValidationException(validationResult.Message);
            }
            
            return Ok(_managementService.ListCommandTemplates(projectId, sessionCode));
        }
        
        /// <summary>
        /// Create Static Command Template
        /// </summary>
        /// <param name="model">CreateCommandTemplateModel</param>
        /// <returns></returns>
        [HttpPost("CommandTemplate")]
        [RequestSizeLimit(1520)]
        [ProducesResponseType(typeof(ExtendedCommandTemplateExt), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status413RequestEntityTooLarge)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public IActionResult CreateCommandTemplate(CreateCommandTemplateModel model)
        {
            _logger.LogDebug($"Endpoint: \"Management\" Method: \"CreateCommandTemplate\"");
            ValidationResult validationResult = new ManagementValidator(model).Validate();
            if (!validationResult.IsValid)
            {
                throw new InputValidationException(validationResult.Message);
            }
            var commandTemplate = _managementService.CreateCommandTemplateModel(model.Name, model.Description, model.ExtendedAllocationCommand, model.ExecutableFile, model.PreparationScript, model.ProjectId, model.ClusterNodeTypeId, model.SessionCode);
            List<ExtendedCommandTemplateParameterExt> templateParameters = new();
            foreach (var templateParameter in model.TemplateParameters)
            {
                var createdTemplateParameter = _managementService.CreateCommandTemplateParameter(templateParameter.Identifier, templateParameter.Query,
                    templateParameter.Description, commandTemplate.Id.Value, model.SessionCode);
                templateParameters.Add(createdTemplateParameter);
            }
            commandTemplate.TemplateParameters = templateParameters.ToArray();
            ClearListAvailableClusterMethodCache();
            return Ok(commandTemplate);
        }
        
        /// <summary>
        /// Create Command Template from Generic Command Template
        /// </summary>
        /// <param name="fromGenericModel">CreateCommandTemplateFromGenericModel</param>
        /// <returns></returns>
        [HttpPost("CommandTemplateFromGeneric")]
        [RequestSizeLimit(1520)]
        [ProducesResponseType(typeof(CommandTemplateExt), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status413RequestEntityTooLarge)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public IActionResult CreateCommandTemplateFromGeneric(CreateCommandTemplateFromGenericModel fromGenericModel)
        {
            _logger.LogDebug($"Endpoint: \"Management\" Method: \"CreateCommandTemplateFromGeneric\"");
            ValidationResult validationResult = new ManagementValidator(fromGenericModel).Validate();
            if (!validationResult.IsValid)
            {
                throw new InputValidationException(validationResult.Message);
            }
            var commandTemplate = _managementService.CreateCommandTemplateFromGeneric(fromGenericModel.GenericCommandTemplateId, fromGenericModel.Name, fromGenericModel.ProjectId, fromGenericModel.Description, fromGenericModel.ExtendedAllocationCommand, fromGenericModel.ExecutableFile, fromGenericModel.PreparationScript, fromGenericModel.SessionCode);
            ClearListAvailableClusterMethodCache();
            return Ok(commandTemplate);
        }
        
        /// <summary>
        /// Modify Static Command Template
        /// </summary>
        /// <param name="model">ModifyCommandTemplateModel</param>
        /// <returns></returns>
        [HttpPut("CommandTemplate")]
        [RequestSizeLimit(1520)]
        [ProducesResponseType(typeof(ExtendedCommandTemplateExt), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status413RequestEntityTooLarge)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public IActionResult ModifyCommandTemplate(ModifyCommandTemplateModel model)
        {
            _logger.LogDebug($"Endpoint: \"Management\" Method: \"ModifyCommandTemplate\"");
            ValidationResult validationResult = new ManagementValidator(model).Validate();
            if (!validationResult.IsValid)
            {
                throw new InputValidationException(validationResult.Message);
            }
            var commandTemplate = _managementService.ModifyCommandTemplateModel(model.Id, model.Name, model.Description, model.ExtendedAllocationCommand, model.ExecutableFile, model.PreparationScript, model.ClusterNodeTypeId, model.SessionCode);
            ClearListAvailableClusterMethodCache();
            return Ok(commandTemplate);
        }

        /// <summary>
        /// Modify Command Template based on Generic Command Template
        /// </summary>
        /// <param name="fromGenericModel">ModifyCommandTemplateFromGenericModel</param>
        /// <returns></returns>
        [HttpPut("CommandTemplateFromGeneric")]
        [RequestSizeLimit(1520)]
        [ProducesResponseType(typeof(CommandTemplateExt), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status413RequestEntityTooLarge)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public IActionResult ModifyCommandTemplateFromGeneric(ModifyCommandTemplateFromGenericModel fromGenericModel)
        {
            _logger.LogDebug($"Endpoint: \"Management\" Method: \"ModifyCommandTemplateFromGeneric\"");
            ValidationResult validationResult = new ManagementValidator(fromGenericModel).Validate();
            if (!validationResult.IsValid)
            {
                throw new InputValidationException(validationResult.Message);
            }
            var commandTemplate = _managementService.ModifyCommandTemplateFromGeneric(fromGenericModel.CommandTemplateId, fromGenericModel.Name, fromGenericModel.ProjectId, fromGenericModel.Description, fromGenericModel.ExtendedAllocationCommand,
                fromGenericModel.ExecutableFile, fromGenericModel.PreparationScript, fromGenericModel.SessionCode);
            ClearListAvailableClusterMethodCache();
            return Ok(commandTemplate);
        }

        /// <summary>
        /// Remove Command Template
        /// </summary>
        /// <param name="model">RemoveCommandTemplateModel</param>
        /// <returns></returns>
        [HttpDelete("RemoveCommandTemplate")]
        [RequestSizeLimit(90)]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status413RequestEntityTooLarge)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public IActionResult RemoveCommandTemplate(RemoveCommandTemplateModel model)
        {
            _logger.LogDebug($"Endpoint: \"Management\" Method: \"RemoveCommandTemplateModel\"");
            ValidationResult validationResult = new ManagementValidator(model).Validate();
            if (!validationResult.IsValid)
            {
                throw new InputValidationException(validationResult.Message);
            }

            ClearListAvailableClusterMethodCache();
            _managementService.RemoveCommandTemplate(model.CommandTemplateId, model.SessionCode);
            return Ok("CommandTemplate was deleted.");
        }
        #endregion

        #region CommandTemplateParameter

        /// <summary>
        /// Create Static Command Template
        /// </summary>
        /// <param name="model">CreateCommandTemplateModel</param>
        /// <returns></returns>
        [HttpPost("CommandTemplateParameter")]
        [RequestSizeLimit(500)]
        [ProducesResponseType(typeof(ExtendedCommandTemplateParameterExt), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status413RequestEntityTooLarge)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public IActionResult CreateCommandTemplateParameter(CreateCommandTemplateParameterModel model)
        {
            _logger.LogDebug($"Endpoint: \"Management\" Method: \"CreateCommandTemplateParameter\"");
            ValidationResult validationResult = new ManagementValidator(model).Validate();
            if (!validationResult.IsValid)
            {
                throw new InputValidationException(validationResult.Message);
            }
            var commandTemplateParameter = _managementService.CreateCommandTemplateParameter(model.Identifier, model.Query, model.Description, model.CommandTemplateId, model.SessionCode);
            ClearListAvailableClusterMethodCache();
            return Ok(commandTemplateParameter);
        }
        
        /// <summary>
        /// Modify Static Command Template
        /// </summary>
        /// <param name="model">ModifyCommandTemplateParameterModel</param>
        /// <returns></returns>
        [HttpPut("CommandTemplateParameter")]
        [RequestSizeLimit(500)]
        [ProducesResponseType(typeof(ExtendedCommandTemplateParameterExt), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status413RequestEntityTooLarge)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public IActionResult ModifyCommandTemplateParameter(ModifyCommandTemplateParameterModel model)
        {
            _logger.LogDebug($"Endpoint: \"Management\" Method: \"ModifyCommandTemplateParameter\"");
            ValidationResult validationResult = new ManagementValidator(model).Validate();
            if (!validationResult.IsValid)
            {
                throw new InputValidationException(validationResult.Message);
            }
            var commandTemplateParameter = _managementService.ModifyCommandTemplateParameter(model.Id, model.Identifier, model.Query, model.Description, model.SessionCode);
            ClearListAvailableClusterMethodCache();
            return Ok(commandTemplateParameter);
        }
        

        /// <summary>
        /// Remove Static Command Template
        /// </summary>
        /// <param name="model">RemoveCommandTemplateParameterModel</param>
        /// <returns></returns>
        [HttpDelete("CommandTemplateParameter")]
        [RequestSizeLimit(100)]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status413RequestEntityTooLarge)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public IActionResult RemoveCommandTemplateParameter(RemoveCommandTemplateParameterModel model)
        {
            _logger.LogDebug($"Endpoint: \"Management\" Method: \"RemoveCommandTemplateParameter\"");
            ValidationResult validationResult = new ManagementValidator(model).Validate();
            if (!validationResult.IsValid)
            {
                throw new InputValidationException(validationResult.Message);
            }

            string message = _managementService.RemoveCommandTemplateParameter(model.Id, model.SessionCode);
            ClearListAvailableClusterMethodCache();
            return Ok(message);
        }
        
        #endregion
        #region Project
        /// <summary>
        /// Create project
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost("Project")]
        [RequestSizeLimit(600)]
        [ProducesResponseType(typeof(ProjectExt), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status413RequestEntityTooLarge)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public IActionResult CreateProject(CreateProjectModel model)
        {
            _logger.LogDebug($"Endpoint: \"Management\" Method: \"CreateProject\" Parameters: SessionCode: \"{model.SessionCode}\"");
            ValidationResult validationResult = new ManagementValidator(model).Validate();
            if (!validationResult.IsValid)
            {
                throw new InputValidationException(validationResult.Message);
            }

            var project = _managementService.CreateProject(model.AccountingString, (UsageType)model.UsageType,
                model.Name,
                model.Description, model.StartDate, model.EndDate, model.UseAccountingStringForScheduler, model.PIEmail,
                model.SessionCode);
            ClearListAvailableClusterMethodCache();
            return Ok(project);
        }

        /// <summary>
        /// Modify project
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPut("Project")]
        [RequestSizeLimit(600)]
        [ProducesResponseType(typeof(ProjectExt), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status413RequestEntityTooLarge)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public IActionResult ModifyProject(ModifyProjectModel model)
        {
            _logger.LogDebug($"Endpoint: \"Management\" Method: \"ModifyProject\" Parameters: SessionCode: \"{model.SessionCode}\"");
            ValidationResult validationResult = new ManagementValidator(model).Validate();
            if (!validationResult.IsValid)
            {
                throw new InputValidationException(validationResult.Message);
            }

            var project = _managementService.ModifyProject(model.Id, (UsageType)model.UsageType, model.Name, model.Description, model.StartDate, model.EndDate, model.UseAccountingStringForScheduler, model.SessionCode);
            ClearListAvailableClusterMethodCache();
            return Ok(project);
        }

        /// <summary>
        /// Remove project
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpDelete("Project")]
        [RequestSizeLimit(600)]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status413RequestEntityTooLarge)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public IActionResult RemoveProject(RemoveProjectModel model)
        {
            _logger.LogDebug($"Endpoint: \"Management\" Method: \"RemoveProject\" Parameters: SessionCode: \"{model.SessionCode}\"");
            ValidationResult validationResult = new ManagementValidator(model).Validate();
            if (!validationResult.IsValid)
            {
                throw new InputValidationException(validationResult.Message);
            }

            ClearListAvailableClusterMethodCache();
            _managementService.RemoveProject(model.Id, model.SessionCode);
            return Ok("Project was deleted.");
        }
        #endregion
        #region ProjectAssignmentToCluster
        /// <summary>
        /// Assign project to the cluster
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost("ProjectAssignmentToCluster")]
        [RequestSizeLimit(600)]
        [ProducesResponseType(typeof(ClusterProjectExt), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status413RequestEntityTooLarge)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public IActionResult CreateProjectAssignmentToCluster(CreateProjectAssignmentToClusterModel model)
        {
            _logger.LogDebug($"Endpoint: \"Management\" Method: \"CreateProjectAssignmentToCluster\" Parameters: SessionCode: \"{model.SessionCode}\"");
            ValidationResult validationResult = new ManagementValidator(model).Validate();
            if (!validationResult.IsValid)
            {
                throw new InputValidationException(validationResult.Message);
            }

            var clusterProject = _managementService.CreateProjectAssignmentToCluster(model.ProjectId, model.ClusterId, model.LocalBasepath, model.SessionCode);
            ClearListAvailableClusterMethodCache();
            return Ok(clusterProject);
        }

        /// <summary>
        /// Modify project assignment to the cluster
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPut("ProjectAssignmentToCluster")]
        [RequestSizeLimit(600)]
        [ProducesResponseType(typeof(ClusterProjectExt), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status413RequestEntityTooLarge)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public IActionResult ModifyProjectAssignmentToCluster(ModifyProjectAssignmentToClusterModel model)
        {
            _logger.LogDebug($"Endpoint: \"Management\" Method: \"ModifyProjectAssignmentToCluster\" Parameters: SessionCode: \"{model.SessionCode}\"");
            ValidationResult validationResult = new ManagementValidator(model).Validate();
            if (!validationResult.IsValid)
            {
                throw new InputValidationException(validationResult.Message);
            }

            var clusterProject = _managementService.ModifyProjectAssignmentToCluster(model.ProjectId, model.ClusterId,
                model.LocalBasepath, model.SessionCode);
            ClearListAvailableClusterMethodCache();
            return Ok(clusterProject);
        }

        /// <summary>
        /// Remove project assignment to the cluster
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpDelete("ProjectAssignmentToCluster")]
        [RequestSizeLimit(100)]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status413RequestEntityTooLarge)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public IActionResult RemoveProjectAssignmentToCluster(RemoveProjectAssignmentToClusterModel model)
        {
            _logger.LogDebug($"Endpoint: \"Management\" Method: \"RemoveProjectAssignmentToCluster\" Parameters: SessionCode: \"{model.SessionCode}\"");
            ValidationResult validationResult = new ManagementValidator(model).Validate();
            if (!validationResult.IsValid)
            {
                throw new InputValidationException(validationResult.Message);
            }

            _managementService.RemoveProjectAssignmentToCluster(model.ProjectId, model.ClusterId, model.SessionCode);
            ClearListAvailableClusterMethodCache();
            return Ok("Removed assignment of the Project to the Cluster.");
        }
        #endregion

        #region SubProject
        /// <summary>
        /// List SubProjects
        /// </summary>
        /// <param name="projectId"></param>
        /// <param name="sessionCode"></param>
        /// <returns></returns>
        /// <exception cref="InputValidationException"></exception>
        [HttpGet("SubProjects")]
        [RequestSizeLimit(100)]
        [ProducesResponseType(typeof(SubProjectExt), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status413RequestEntityTooLarge)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public IActionResult ListSubProjects(long projectId, string sessionCode)
        {
            _logger.LogDebug($"Endpoint: \"Management\" Method: \"ListSubProjects\" Parameters: SessionCode: \"{sessionCode}\"");
            var model = new ListSubProjectsModel()
            {
                Id = projectId,
                SessionCode = sessionCode
            };
            ValidationResult validationResult = new ManagementValidator(model).Validate();
            if (!validationResult.IsValid)
            {
                throw new InputValidationException(validationResult.Message);
            }

            return Ok(_managementService.ListSubProjects(projectId, sessionCode));
        }
        /// <summary>
        /// List SubProject
        /// </summary>
        /// <param name="subProjectId"></param>
        /// <param name="sessionCode"></param>
        /// <returns></returns>
        /// <exception cref="InputValidationException"></exception>
        [HttpGet("SubProject")]
        [RequestSizeLimit(100)]
        [ProducesResponseType(typeof(SubProjectExt), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status413RequestEntityTooLarge)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public IActionResult ListSubProject(long subProjectId, string sessionCode)
        {
            _logger.LogDebug($"Endpoint: \"Management\" Method: \"ListSubProject\" Parameters: SessionCode: \"{sessionCode}\"");
            var model = new ListSubProjectModel()
            {
                Id = subProjectId,
                SessionCode = sessionCode
            };
            ValidationResult validationResult = new ManagementValidator(model).Validate();
            if (!validationResult.IsValid)
            {
                throw new InputValidationException(validationResult.Message);
            }

            return Ok(_managementService.ListSubProject(subProjectId, sessionCode));
        }
        
        /// <summary>
        /// Create SubProject
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <exception cref="InputValidationException"></exception>
        [HttpPost("SubProject")]
        [RequestSizeLimit(1000)]
        [ProducesResponseType(typeof(SubProjectExt), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status413RequestEntityTooLarge)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public IActionResult CreateSubProject(CreateSubProjectModel model)
        {
            _logger.LogDebug($"Endpoint: \"Management\" Method: \"CreateSubProject\" Parameters: SessionCode: \"{model.SessionCode}\"");
            ValidationResult validationResult = new ManagementValidator(model).Validate();
            if (!validationResult.IsValid)
            {
                throw new InputValidationException(validationResult.Message);
            }

            var subProject = _managementService.CreateSubProject(model.ProjectId, model.Identifier, model.Description, model.StartDate, model.EndDate, model.SessionCode);
            return Ok(subProject);
        }
        /// <summary>
        /// Modify SubProject
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <exception cref="InputValidationException"></exception>
        [HttpPut("SubProject")]
        [RequestSizeLimit(1000)]
        [ProducesResponseType(typeof(SubProjectExt), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status413RequestEntityTooLarge)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public IActionResult ModifySubProject(ModifySubProjectModel model)
        {
            _logger.LogDebug($"Endpoint: \"Management\" Method: \"ModifySubProject\" Parameters: SessionCode: \"{model.SessionCode}\"");
            ValidationResult validationResult = new ManagementValidator(model).Validate();
            if (!validationResult.IsValid)
            {
                throw new InputValidationException(validationResult.Message);
            }

            var subProject = _managementService.ModifySubProject(model.Id, model.Identifier, model.Description, model.StartDate, model.EndDate, model.SessionCode);
            return Ok(subProject);
        }
        /// <summary>
        /// Remove SubProject
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <exception cref="InputValidationException"></exception>
        [HttpDelete("SubProject")]
        [RequestSizeLimit(100)]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status413RequestEntityTooLarge)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public IActionResult RemoveSubProject(RemoveSubProjectModel model)
        {
            _logger.LogDebug($"Endpoint: \"Management\" Method: \"RemoveSubProject\" Parameters: SessionCode: \"{model.SessionCode}\"");
            ValidationResult validationResult = new ManagementValidator(model).Validate();
            if (!validationResult.IsValid)
            {
                throw new InputValidationException(validationResult.Message);
            }

            _managementService.RemoveSubProject(model.Id, model.SessionCode);
            return Ok("SubProject was deleted.");
        }

        #endregion
        #region SecureShellKey
        /// <summary>
        /// Generate SSH key
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost("SecureShellKey")]
        [RequestSizeLimit(300)]
        [ProducesResponseType(typeof(List<PublicKeyExt>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status413RequestEntityTooLarge)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        [Obsolete]
        public IActionResult CreateSecureShellKeyObsolete(CreateSecureShellKeyModelObsolete model)
        {
            _logger.LogDebug($"Endpoint: \"Management\" Method: \"CreateSecureShellKey\"");
            ValidationResult validationResult = new ManagementValidator(model).Validate();
            if (!validationResult.IsValid)
            {
                throw new InputValidationException(validationResult.Message);
            }
            List<(string, string)> usernamePasswords = new()
            {
                (model.Username, model.Password)
            };

            return Ok(_managementService.CreateSecureShellKey(usernamePasswords, model.ProjectId, model.SessionCode));
        }

        /// <summary>
        /// Generate SSH key
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost("GenerateSecureShellKey")]
        [RequestSizeLimit(1000)]
        [ProducesResponseType(typeof(List<PublicKeyExt>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status413RequestEntityTooLarge)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public IActionResult GenerateSecureShellKey(CreateSecureShellKeyModel model)
        {
            _logger.LogDebug($"Endpoint: \"Management\" Method: \"GenerateSecureShellKey\"");
            ValidationResult validationResult = new ManagementValidator(model).Validate();
            if (!validationResult.IsValid)
            {
                throw new InputValidationException(validationResult.Message);
            }

            List<(string, string)> credentials = model.Credentials.Select(credential => (credential.Username, credential.Password)).ToList();
            return Ok(_managementService.CreateSecureShellKey(credentials, model.ProjectId, model.SessionCode));
        }

        /// <summary>
        /// Regenerate SSH key
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPut("SecureShellKey")]
        [RequestSizeLimit(1000)]
        [ProducesResponseType(typeof(PublicKeyExt), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status413RequestEntityTooLarge)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        [Obsolete]
        public IActionResult RecreateSecureShellKey(RegenerateSecureShellKeyModelObsolete model)
        {
            _logger.LogDebug($"Endpoint: \"Management\" Method: \"RecreateSecureShellKey\"");
            ValidationResult validationResult = new ManagementValidator(model).Validate();
            if (!validationResult.IsValid)
            {
                throw new InputValidationException(validationResult.Message);
            }

            return Ok(_managementService.RegenerateSecureShellKey(string.Empty, model.Password, model.PublicKey, model.ProjectId, model.SessionCode));
        }

        /// <summary>
        /// Regenerate SSH key
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPut("RegenerateSecureShellKey")]
        [RequestSizeLimit(1000)]
        [ProducesResponseType(typeof(PublicKeyExt), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status413RequestEntityTooLarge)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public IActionResult RegenerateSecureShellKey(RegenerateSecureShellKeyModel model)
        {
            _logger.LogDebug($"Endpoint: \"Management\" Method: \"RecreateSecureShellKey\"");
            ValidationResult validationResult = new ManagementValidator(model).Validate();
            if (!validationResult.IsValid)
            {
                throw new InputValidationException(validationResult.Message);
            }

            return Ok(_managementService.RegenerateSecureShellKey(model.Username, model.Password, string.Empty, model.ProjectId, model.SessionCode));
        }

        /// <summary>
        /// Remove SSH key
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpDelete("SecureShellKey")]
        [RequestSizeLimit(1000)]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status413RequestEntityTooLarge)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        [Obsolete]
        public IActionResult RemoveSecureShellKeyObsolete(RemoveSecureShellKeyModelObsolete model)
        {
            _logger.LogDebug($"Endpoint: \"Management\" Method: \"RevokeSecureShellKey\"");
            ValidationResult validationResult = new ManagementValidator(model).Validate();
            if (!validationResult.IsValid)
            {
                throw new InputValidationException(validationResult.Message);
            }

            _managementService.RemoveSecureShellKey(null, model.PublicKey, model.ProjectId, model.SessionCode);
            return Ok("SecureShellKey revoked");
        }

        /// <summary>
        /// Remove SSH key
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpDelete("RemoveSecureShellKey")]
        [RequestSizeLimit(1000)]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status413RequestEntityTooLarge)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public IActionResult RemoveSecureShellKey(RemoveSecureShellKeyModel model)
        {
            _logger.LogDebug($"Endpoint: \"Management\" Method: \"RevokeSecureShellKey\"");
            ValidationResult validationResult = new ManagementValidator(model).Validate();
            if (!validationResult.IsValid)
            {
                throw new InputValidationException(validationResult.Message);
            }

            _managementService.RemoveSecureShellKey(model.Username, null, model.ProjectId, model.SessionCode);
            return Ok("SecureShellKey revoked");
        }
        #endregion
        /// <summary>
        /// Initialize cluster script directory for SSH HPC Account
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost("InitializeClusterScriptDirectory")]
        [RequestSizeLimit(1000)]
        [ProducesResponseType(typeof(List<ClusterInitReportExt>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status413RequestEntityTooLarge)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public IActionResult InitializeClusterScriptDirectory(InitializeClusterScriptDirectoryModel model)
        {
            _logger.LogDebug($"Endpoint: \"Management\" Method: \"InitializeClusterScriptDirectory\"");
            ValidationResult validationResult = new ManagementValidator(model).Validate();
            if (!validationResult.IsValid)
            {
                throw new InputValidationException(validationResult.Message);
            }

            return Ok(_managementService.InitializeClusterScriptDirectory(model.ProjectId, model.ClusterProjectRootDirectory, model.SessionCode));
        }

        /// <summary>
        /// Test cluster access for robot account
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost("TestClusterAccessForAccount")]
        [RequestSizeLimit(1000)]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status413RequestEntityTooLarge)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        [Obsolete]
        public IActionResult TestClusterAccessForAccountObsolete(TestClusterAccessForAccountModelObsolete model)
        {
            _logger.LogDebug($"Endpoint: \"Management\" Method: \"TestClusterAccessForAccount\"");
            ValidationResult validationResult = new ManagementValidator(model).Validate();
            if (!validationResult.IsValid)
            {
                throw new InputValidationException(validationResult.Message);
            }

            bool testClusterAccess = _managementService.TestClusterAccessForAccount(model.ProjectId, model.SessionCode, null);
            var message = testClusterAccess
                ? "All clusters assigned to project are accessible with selected account."
                : "Some of the clusters are not accessible with selected account";

            _logger.LogInformation(message);
            return testClusterAccess ? Ok(message) : BadRequest(message);
        }

        /// <summary>
        /// Test cluster access for robot account
        /// </summary>
        /// <param name="username"></param>
        /// <param name="projectId"></param>
        /// <param name="sessionCode"></param>
        /// <returns></returns>
        [HttpGet("TestClusterAccessForAccount")]
        [RequestSizeLimit(1000)]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status413RequestEntityTooLarge)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public IActionResult TestClusterAccessForAccount(string username, long projectId, string sessionCode)
        {
            _logger.LogDebug($"Endpoint: \"Management\" Method: \"TestClusterAccessForAccount\"");

            ValidationResult validationResult = new ManagementValidator(new TestClusterAccessForAccountModel()
            {
                ProjectId = projectId,
                SessionCode = sessionCode,
                Username = username
            }).Validate();
            if (!validationResult.IsValid)
            {
                throw new InputValidationException(validationResult.Message);
            }

            bool testClusterAccess = _managementService.TestClusterAccessForAccount(projectId, sessionCode, username);
            var message = testClusterAccess
                ? "All clusters assigned to project are accessible with selected account."
                : "Some of the clusters are not accessible with selected account";

            _logger.LogInformation(message);
            return testClusterAccess ? Ok(message) : BadRequest(message);
        }
        
        /// <summary>
        /// Compute accounting - calculate accounting via accounting formulas
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <exception cref="InputValidationException"></exception>
        [HttpPost("ComputeAccounting")]
        [RequestSizeLimit(500)]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status413RequestEntityTooLarge)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public IActionResult ComputeAccounting(ComputeAccountingModel model)
        {
            _logger.LogDebug($"Endpoint: \"Management\" Method: \"ComputeAccounting\"");

            ValidationResult validationResult = new ManagementValidator(model).Validate();
            if (!validationResult.IsValid)
            {
                throw new InputValidationException(validationResult.Message);
            }

            _managementService.ComputeAccounting(model.StartTime, model.EndTime, model.ProjectId, model.SessionCode);
            return Ok($"Accounting triggered for project {model.ProjectId}.");
        }
        
        
        [HttpPost("AccountingState")]
        [RequestSizeLimit(200)]
        [ProducesResponseType(typeof(AccountingStateExt), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status413RequestEntityTooLarge)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public IActionResult ListAccountingStates(long projectId, string sessionCode)
        {
            _logger.LogDebug($"Endpoint: \"Management\" Method: \"AccountingState\"");
            var model = new AccountingStateModel()
            {
                ProjectId = projectId,
                SessionCode = sessionCode
            };
            ValidationResult validationResult = new ManagementValidator(model).Validate();
            if (!validationResult.IsValid)
            {
                throw new InputValidationException(validationResult.Message);
            }
            return Ok(_managementService.ListAccountingStates(projectId, sessionCode));
        }

        #endregion
        #region Private Methods
        private void ClearListAvailableClusterMethodCache()
        {
            string memoryCacheKey = nameof(ClusterInformationController.ListAvailableClusters);
            _cacheProvider.RemoveKeyFromCache(_logger, memoryCacheKey, nameof(CreateProjectAssignmentToCluster));
        }
        #endregion
    }
}