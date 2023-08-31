﻿using Exceptions.External;
using HEAppE.DataAccessTier.UnitOfWork;
using HEAppE.DomainObjects.ClusterInformation;
using HEAppE.DomainObjects.JobManagement;
using HEAppE.HpcConnectionFramework.SchedulerAdapters;
using System.Collections.Generic;
using System.Linq;

namespace HEAppE.BusinessLogicTier.Logic.Management
{
    public class ManagementLogic : IManagementLogic
    {
        protected IUnitOfWork _unitOfWork;
        public ManagementLogic(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public CommandTemplate CreateCommandTemplate(long genericCommandTemplateId, string name, long projectId, string description, string extendedAllocationCommand, string executableFile, string preparationScript)
        {
            CommandTemplate commandTemplate = _unitOfWork.CommandTemplateRepository.GetById(genericCommandTemplateId);
            if (commandTemplate is null)
            {
                throw new RequestedObjectDoesNotExistException("CommandTemplateNotFound");
            }

            if (!commandTemplate.IsGeneric)
            {
                throw new InputValidationException("CommandTemplateNotGeneric");
            }

            if (!commandTemplate.IsEnabled)
            {
                throw new InputValidationException("CommandTemplateDeleted");
            }

            var commandTemplateParameter = commandTemplate.TemplateParameters.Where(w => w.IsVisible)
                                                                               .FirstOrDefault();

            if (string.IsNullOrEmpty(commandTemplateParameter?.Identifier))
            {
                throw new RequestedObjectDoesNotExistException("UserScriptNotDefined");
            }

            if (string.IsNullOrEmpty(executableFile))
            {
                throw new InputValidationException("NoScriptPath");
            }

            Cluster cluster = commandTemplate.ClusterNodeType.Cluster;
            var serviceAccount = _unitOfWork.ClusterAuthenticationCredentialsRepository.GetServiceAccountCredentials(cluster.Id, projectId);
            var commandTemplateParameters = SchedulerFactory.GetInstance(cluster.SchedulerType)
                                                             .CreateScheduler(cluster)
                                                             .GetParametersFromGenericUserScript(cluster, serviceAccount, executableFile)
                                                             .ToList();

            List<CommandTemplateParameter> templateParameters = new();
            foreach (string parameter in commandTemplateParameters)
            {
                templateParameters.Add(new CommandTemplateParameter()
                {
                    Identifier = parameter,
                    Description = parameter,
                    Query = string.Empty
                });
            }

            CommandTemplate newCommandTemplate = new()
            {
                Name = name,
                Description = description,
                IsGeneric = false,
                IsEnabled = true,
                ClusterNodeType = commandTemplate.ClusterNodeType,
                ClusterNodeTypeId = commandTemplate.ClusterNodeTypeId,
                ExtendedAllocationCommand = extendedAllocationCommand,
                ExecutableFile = executableFile,
                PreparationScript = preparationScript,
                TemplateParameters = templateParameters,
                CommandParameters = string.Join(' ', commandTemplateParameters.Select(x => $"%%{"{"}{x}{"}"}"))
            };

            _unitOfWork.CommandTemplateRepository.Insert(newCommandTemplate);
            _unitOfWork.Save();

            return newCommandTemplate;
        }

        public CommandTemplate ModifyCommandTemplate(long commandTemplateId, string name, long projectId, string description, string extendedAllocationCommand, string executableFile, string preparationScript)
        {
            CommandTemplate commandTemplate = _unitOfWork.CommandTemplateRepository.GetById(commandTemplateId);
            if (commandTemplate is null)
            {
                throw new RequestedObjectDoesNotExistException("CommandTemplateNotFound");
            }

            if (!commandTemplate.IsEnabled)
            {
                throw new InputValidationException("CommandTemplateDeleted");
            }

            if (commandTemplate.IsGeneric)
            {
                throw new InputValidationException("CommandTemplateIsGeneric");
            }

            if (executableFile is null)
            {
                throw new InputValidationException("CommandTemplateNoExecutableFile");
            }

            Cluster cluster = commandTemplate.ClusterNodeType.Cluster;
            var serviceAccount = _unitOfWork.ClusterAuthenticationCredentialsRepository.GetServiceAccountCredentials(cluster.Id, projectId);
            var commandTemplateParameters = SchedulerFactory.GetInstance(cluster.SchedulerType)
                                                             .CreateScheduler(cluster)
                                                             .GetParametersFromGenericUserScript(cluster, serviceAccount, executableFile)
                                                             .ToList();

            var templateParameters = new List<CommandTemplateParameter>();
            foreach (string parameter in commandTemplateParameters)
            {
                templateParameters.Add(new CommandTemplateParameter()
                {
                    Identifier = parameter,
                    Description = parameter,
                    Query = string.Empty
                });
            }

            commandTemplate.Name = name;
            commandTemplate.Description = description;
            commandTemplate.ExtendedAllocationCommand = extendedAllocationCommand;
            commandTemplate.PreparationScript = preparationScript;
            commandTemplate.TemplateParameters.ForEach(cmdParameters => _unitOfWork.CommandTemplateParameterRepository.Delete(cmdParameters));
            commandTemplate.TemplateParameters.AddRange(templateParameters);
            commandTemplate.ExecutableFile = executableFile;
            commandTemplate.CommandParameters = string.Join(' ', commandTemplateParameters.Select(x => $"%%{"{"}{x}{"}"}"));

            _unitOfWork.Save();
            return commandTemplate;
        }

        public void RemoveCommandTemplate(long commandTemplateId)
        {
            CommandTemplate commandTemplate = _unitOfWork.CommandTemplateRepository.GetById(commandTemplateId);
            if (commandTemplate == null)
            {
                throw new RequestedObjectDoesNotExistException("CommandTemplateNotFound");
            }

            commandTemplate.IsEnabled = false;
            _unitOfWork.Save();
        }

    }
}
