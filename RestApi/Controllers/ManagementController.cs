using HEAppE.BusinessLogicTier.Logic;
using HEAppE.DataAccessTier.Factory.UnitOfWork;
using HEAppE.DataAccessTier.UnitOfWork;
using HEAppE.DomainObjects.JobManagement;
using HEAppE.DomainObjects.JobReporting.Enums;
using HEAppE.ExtModels.ClusterInformation.Models;
using HEAppE.ExtModels.JobManagement.Converts;
using HEAppE.ExtModels.JobManagement.Models;
using HEAppE.ExtModels.Management.Converts;
using HEAppE.ExtModels.Management.Models;
using HEAppE.RestApi.Configuration;
using HEAppE.RestApi.InputValidator;
using HEAppE.RestApiModels.JobManagement;
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
        /// Get HEAppE Infromation
        /// </summary>
        /// <param name="sessionCode">SessionCode</param>
        /// <returns></returns>
        [HttpGet("InstanceInformations")]
        [RequestSizeLimit(90)]
        [ProducesResponseType(typeof(InstanceInformationExt), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        [ProducesResponseType(StatusCodes.Status413RequestEntityTooLarge)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        public IActionResult InstanceInformations(string sessionCode)
        {
            try
            {
                _logger.LogDebug($"Endpoint: \"Management\" Method: \"GetInstanceInformations\" Parameters: SessionCode: \"{sessionCode}\"");
                ValidationResult validationResult = new SessionCodeValidator(sessionCode).Validate();
                if (!validationResult.IsValid)
                {
                    ExceptionHandler.ThrowProperExternalException(new InputValidationException(validationResult.Message));
                }

                var result = _userAndManagementService.ValidateUserPermissions(sessionCode);
                if (result)
                {
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
                else
                {
                    return BadRequest(null);
                }
            }
            catch (Exception exception)
            {
                if (exception is InputValidationException)
                {
                    BadRequest(exception.Message);
                }
                return Problem(null, null, null, exception.Message);
            }
        }
        #endregion
        #region CommandTemplate
        /// <summary>
        /// Create Command Template from Generic Command Template
        /// </summary>
        /// <param name="model">CreateCommandTemplate</param>
        /// <returns></returns>
        [HttpPost("CommandTemplate")]
        [RequestSizeLimit(1520)]
        [ProducesResponseType(typeof(CommandTemplateExt), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        [ProducesResponseType(StatusCodes.Status413RequestEntityTooLarge)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        public IActionResult CreateCommandTemplate(CreateCommandTemplateModel model)
        {
            try
            {
                _logger.LogDebug($"Endpoint: \"Management\" Method: \"CreateCommandTemplate\"");
                ValidationResult validationResult = new ManagementValidator(model).Validate();
                if (!validationResult.IsValid)
                {
                    ExceptionHandler.ThrowProperExternalException(new InputValidationException(validationResult.Message));
                }

                string memoryCacheKey = nameof(ClusterInformationController.ListAvailableClusters);
                _cacheProvider.RemoveKeyFromCache(_logger, memoryCacheKey, nameof(CreateCommandTemplate));

                return Ok(_managementService.CreateCommandTemplate(model.GenericCommandTemplateId, model.Name, model.ProjectId, model.Description, model.Code, model.ExecutableFile, model.PreparationScript, model.SessionCode));
            }
            catch (Exception exception)
            {
                if (exception is InputValidationException)
                {
                    BadRequest(exception.Message);
                }
                return Problem(null, null, null, exception.Message);
            }
        }

        /// <summary>
        /// Modify command template
        /// </summary>
        /// <param name="model">ModifyCommandTemplateModel</param>
        /// <returns></returns>
        [HttpPut("CommandTemplate")]
        [RequestSizeLimit(1520)]
        [ProducesResponseType(typeof(CommandTemplateExt), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        [ProducesResponseType(StatusCodes.Status413RequestEntityTooLarge)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        public IActionResult ModifyCommandTemplate(ModifyCommandTemplateModel model)
        {
            try
            {
                _logger.LogDebug($"Endpoint: \"Management\" Method: \"ModifyCommandTemplate\"");
                ValidationResult validationResult = new ManagementValidator(model).Validate();
                if (!validationResult.IsValid)
                {
                    ExceptionHandler.ThrowProperExternalException(new InputValidationException(validationResult.Message));
                }

                string memoryCacheKey = nameof(ClusterInformationController.ListAvailableClusters);
                _cacheProvider.RemoveKeyFromCache(_logger, memoryCacheKey, nameof(ModifyCommandTemplate));

                return Ok(_managementService.ModifyCommandTemplate(model.CommandTemplateId, model.Name, model.ProjectId, model.Description, model.Code,
                                                         model.ExecutableFile, model.PreparationScript, model.SessionCode));
            }
            catch (Exception exception)
            {
                if (exception is InputValidationException)
                {
                    BadRequest(exception.Message);
                }
                return Problem(null, null, null, exception.Message);
            }
        }

        /// <summary>
        /// Remove command template
        /// </summary>
        /// <param name="model">RemoveCommandTemplateModel</param>
        /// <returns></returns>
        [HttpDelete("CommandTemplate")]
        [RequestSizeLimit(90)]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        [ProducesResponseType(StatusCodes.Status413RequestEntityTooLarge)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        public IActionResult RemoveCommandTemplate(RemoveCommandTemplateModel model)
        {
            try
            {
                _logger.LogDebug($"Endpoint: \"Management\" Method: \"RemoveCommandTemplate\"");
                ValidationResult validationResult = new ManagementValidator(model).Validate();
                if (!validationResult.IsValid)
                {
                    ExceptionHandler.ThrowProperExternalException(new InputValidationException(validationResult.Message));
                }

                string memoryCacheKey = nameof(ClusterInformationController.ListAvailableClusters);
                _cacheProvider.RemoveKeyFromCache(_logger, memoryCacheKey, nameof(RemoveCommandTemplate));

                return Ok(_managementService.RemoveCommandTemplate(model.CommandTemplateId, model.SessionCode));
            }
            catch (Exception exception)
            {
                if (exception is InputValidationException)
                {
                    BadRequest(exception.Message);
                }
                return Problem(null, null, null, exception.Message);
            }
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
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        [ProducesResponseType(StatusCodes.Status413RequestEntityTooLarge)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        public IActionResult CreateProject(CreateProjectModel model)
        {
            try
            {
                _logger.LogDebug($"Endpoint: \"Management\" Method: \"CreateProject\" Parameters: SessionCode: \"{model.SessionCode}\"");
                ValidationResult validationResult = new ManagementValidator(model).Validate();
                if (!validationResult.IsValid)
                {
                    ExceptionHandler.ThrowProperExternalException(new InputValidationException(validationResult.Message));
                }

                string memoryCacheKey = nameof(ClusterInformationController.ListAvailableClusters);
                _cacheProvider.RemoveKeyFromCache(_logger, memoryCacheKey, nameof(CreateProjectAssignmentToCluster));

                return Ok(_managementService.CreateProject(model.AccountingString, (UsageType)model.UsageType, model.Name, model.Description, model.StartDate, model.EndDate, model.SessionCode));
            }
            catch (Exception exception)
            {
                if (exception is InputValidationException)
                {
                    BadRequest(exception.Message);
                }
                return Problem(null, null, null, exception.Message);
            }
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
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        [ProducesResponseType(StatusCodes.Status413RequestEntityTooLarge)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        public IActionResult ModifyProject(ModifyProjectModel model)
        {
            try
            {
                _logger.LogDebug($"Endpoint: \"Management\" Method: \"ModifyProject\" Parameters: SessionCode: \"{model.SessionCode}\"");
                ValidationResult validationResult = new ManagementValidator(model).Validate();
                if (!validationResult.IsValid)
                {
                    ExceptionHandler.ThrowProperExternalException(new InputValidationException(validationResult.Message));
                }

                string memoryCacheKey = nameof(ClusterInformationController.ListAvailableClusters);
                _cacheProvider.RemoveKeyFromCache(_logger, memoryCacheKey, nameof(CreateProjectAssignmentToCluster));

                return Ok(_managementService.ModifyProject(model.Id, (UsageType)model.UsageType, model.Name, model.Description, model.StartDate, model.EndDate, model.SessionCode));
            }
            catch (Exception exception)
            {
                if (exception is InputValidationException)
                {
                    BadRequest(exception.Message);
                }
                return Problem(null, null, null, exception.Message);
            }
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
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        [ProducesResponseType(StatusCodes.Status413RequestEntityTooLarge)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        public IActionResult RemoveProject(RemoveProjectModel model)
        {
            try
            {
                _logger.LogDebug($"Endpoint: \"Management\" Method: \"RemoveProject\" Parameters: SessionCode: \"{model.SessionCode}\"");
                ValidationResult validationResult = new ManagementValidator(model).Validate();
                if (!validationResult.IsValid)
                {
                    ExceptionHandler.ThrowProperExternalException(new InputValidationException(validationResult.Message));
                }

                string memoryCacheKey = nameof(ClusterInformationController.ListAvailableClusters);
                _cacheProvider.RemoveKeyFromCache(_logger, memoryCacheKey, nameof(CreateProjectAssignmentToCluster));

                return Ok(_managementService.RemoveProject(model.Id, model.SessionCode));
            }
            catch (Exception exception)
            {
                if (exception is InputValidationException)
                {
                    BadRequest(exception.Message);
                }
                return Problem(null, null, null, exception.Message);
            }
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
        [ProducesResponseType(typeof(ClusterProject), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        [ProducesResponseType(StatusCodes.Status413RequestEntityTooLarge)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        public IActionResult CreateProjectAssignmentToCluster(CreateProjectAssignmentToClusterModel model)
        {
            try
            {
                _logger.LogDebug($"Endpoint: \"Management\" Method: \"CreateProjectAssignmentToCluster\" Parameters: SessionCode: \"{model.SessionCode}\"");
                ValidationResult validationResult = new ManagementValidator(model).Validate();
                if (!validationResult.IsValid)
                {
                    ExceptionHandler.ThrowProperExternalException(new InputValidationException(validationResult.Message));
                }

                string memoryCacheKey = nameof(ClusterInformationController.ListAvailableClusters);
                _cacheProvider.RemoveKeyFromCache(_logger, memoryCacheKey, nameof(CreateProjectAssignmentToCluster));

                return Ok(_managementService.CreateProjectAssignmentToCluster(model.ProjectId, model.ClusterId, model.LocalBasepath, model.SessionCode));
            }
            catch (Exception exception)
            {
                if (exception is InputValidationException)
                {
                    BadRequest(exception.Message);
                }
                return Problem(null, null, null, exception.Message);
            }
        }

        /// <summary>
        /// Modify project assignment to the cluster
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPut("ProjectAssignmentToCluster")]
        [RequestSizeLimit(600)]
        [ProducesResponseType(typeof(ClusterProject), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        [ProducesResponseType(StatusCodes.Status413RequestEntityTooLarge)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        public IActionResult ModifyProjectAssignmentToCluster(ModifyProjectAssignmentToClusterModel model)
        {
            try
            {
                _logger.LogDebug($"Endpoint: \"Management\" Method: \"ModifyProjectAssignmentToCluster\" Parameters: SessionCode: \"{model.SessionCode}\"");
                ValidationResult validationResult = new ManagementValidator(model).Validate();
                if (!validationResult.IsValid)
                {
                    ExceptionHandler.ThrowProperExternalException(new InputValidationException(validationResult.Message));
                }

                string memoryCacheKey = nameof(ClusterInformationController.ListAvailableClusters);
                _cacheProvider.RemoveKeyFromCache(_logger, memoryCacheKey, nameof(CreateProjectAssignmentToCluster));

                return Ok(_managementService.ModifyProjectAssignmentToCluster(model.ProjectId, model.ClusterId, model.LocalBasepath, model.SessionCode));
            }
            catch (Exception exception)
            {
                if (exception is InputValidationException)
                {
                    BadRequest(exception.Message);
                }
                return Problem(null, null, null, exception.Message);
            }
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
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        [ProducesResponseType(StatusCodes.Status413RequestEntityTooLarge)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        public IActionResult RemoveProjectAssignmentToCluster(RemoveProjectAssignmentToClusterModel model)
        {
            try
            {
                _logger.LogDebug($"Endpoint: \"Management\" Method: \"RemoveProjectAssignmentToCluster\" Parameters: SessionCode: \"{model.SessionCode}\"");
                ValidationResult validationResult = new ManagementValidator(model).Validate();
                if (!validationResult.IsValid)
                {
                    ExceptionHandler.ThrowProperExternalException(new InputValidationException(validationResult.Message));
                }

                string memoryCacheKey = nameof(ClusterInformationController.ListAvailableClusters);
                _cacheProvider.RemoveKeyFromCache(_logger, memoryCacheKey, nameof(CreateProjectAssignmentToCluster));

                return Ok(_managementService.RemoveProjectAssignmentToCluster(model.ProjectId, model.ClusterId, model.SessionCode));
            }
            catch (Exception exception)
            {
                if (exception is InputValidationException)
                {
                    BadRequest(exception.Message);
                }
                return Problem(null, null, null, exception.Message);
            }
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
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        [ProducesResponseType(StatusCodes.Status413RequestEntityTooLarge)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        public IActionResult CreateSecureShellKey(CreateSecureShellKeyModel model)
        {
            try
            {
                _logger.LogDebug($"Endpoint: \"Management\" Method: \"CreateSecureShellKey\"");
                ValidationResult validationResult = new ManagementValidator(model).Validate();
                if (!validationResult.IsValid)
                {
                    ExceptionHandler.ThrowProperExternalException(new InputValidationException(validationResult.Message));
                }
                return Ok(_managementService.CreateSecureShellKey(model.Username, model.Password, model.ProjectId, model.SessionCode));
            }
            catch (Exception exception)
            {
                if (exception is InputValidationException)
                {
                    BadRequest(exception.Message);
                }
                return Problem(null, null, null, exception.Message);
            }
        }

        /// <summary>
        /// Regenerate SSH key
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPut("SecureShellKey")]
        [RequestSizeLimit(1000)]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        [ProducesResponseType(StatusCodes.Status413RequestEntityTooLarge)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        public IActionResult RecreateSecureShellKey(RecreateSecureShellKeyModel model)
        {
            try
            {
                _logger.LogDebug($"Endpoint: \"Management\" Method: \"RecreateSecureShellKey\"");
                ValidationResult validationResult = new ManagementValidator(model).Validate();
                if (!validationResult.IsValid)
                {
                    ExceptionHandler.ThrowProperExternalException(new InputValidationException(validationResult.Message));
                }
                return Ok(_managementService.RecreateSecureShellKey(model.Username, model.Password, model.PublicKey, model.ProjectId, model.SessionCode));
            }
            catch (Exception exception)
            {
                if (exception is InputValidationException)
                {
                    BadRequest(exception.Message);
                }
                return Problem(null, null, null, exception.Message);
            }
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
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        [ProducesResponseType(StatusCodes.Status413RequestEntityTooLarge)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        public IActionResult RemoveSecureShellKey(RemoveSecureShellKeyModel model)
        {
            try
            {
                _logger.LogDebug($"Endpoint: \"Management\" Method: \"RevokeSecureShellKey\"");
                ValidationResult validationResult = new ManagementValidator(model).Validate();
                if (!validationResult.IsValid)
                {
                    ExceptionHandler.ThrowProperExternalException(new InputValidationException(validationResult.Message));
                }
                return Ok(_managementService.RemoveSecureShellKey(model.PublicKey, model.ProjectId, model.SessionCode));
            }
            catch (Exception exception)
            {
                if (exception is InputValidationException)
                {
                    BadRequest(exception.Message);
                }
                return Problem(null, null, null, exception.Message);
            }
        }

        public IActionResult TestClusterAccessForAccount(TestClusterAccessForAccountModel model)
        {
            try
            {
                _logger.LogDebug($"Endpoint: \"Management\" Method: \"TestClusterAccessForAccount\"");
                ValidationResult validationResult = new ManagementValidator(model).Validate();
                if (!validationResult.IsValid)
                {
                    ExceptionHandler.ThrowProperExternalException(new InputValidationException(validationResult.Message)); 
                }
                return Ok(_managementService.TestClusterAccessForAccount(model.ProjectId, model.PublicKey, model.SessionCode));
            }
            catch (Exception exception)
            {
                return Problem(null, null, null, exception.Message);
            }
        }
        #endregion
        
        
        /// <summary>
        /// Initialize cluster script directory for SSH HPC Account
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost("InitializeClusterScriptDirectory")]
        [RequestSizeLimit(1000)]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        [ProducesResponseType(StatusCodes.Status413RequestEntityTooLarge)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        public IActionResult InitializeClusterScriptDirectory(InitializeClusterScriptDirectoryModel model)
        {
            try
            {
                _logger.LogDebug($"Endpoint: \"Management\" Method: \"InitializeClusterScriptDirectory\"");
                ValidationResult validationResult = new ManagementValidator(model).Validate();
                if (!validationResult.IsValid)
                {
                    ExceptionHandler.ThrowProperExternalException(new InputValidationException(validationResult.Message));
                }
                return Ok(_managementService.InitializeClusterScriptDirectory(model.ProjectId, model.PublicKey, model.ClusterProjectRootDirectory, model.SessionCode));
            }
            catch (Exception exception)
            {
                if (exception is InputValidationException)
                {
                    BadRequest(exception.Message);
                }
                return Problem(null, null, null, exception.Message);
            }
        }
        #endregion
    }
}
