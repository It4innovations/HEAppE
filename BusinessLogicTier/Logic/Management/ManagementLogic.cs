using HEAppE.BusinessLogicTier.Logic.Management.Exceptions;
using HEAppE.DataAccessTier.UnitOfWork;
using HEAppE.DomainObjects.ClusterInformation;
using HEAppE.DomainObjects.JobManagement;
using HEAppE.HpcConnectionFramework.SchedulerAdapters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HEAppE.BusinessLogicTier.Logic.Management
{
    internal class ManagementLogic : IManagementLogic
    {
        protected IUnitOfWork unitOfWork;
        internal ManagementLogic(IUnitOfWork unitOfWork)
        {
            this.unitOfWork = unitOfWork;
        }

        public CommandTemplate CreateCommandTemplate(long genericCommandTemplateId, string name, string description, string code, string executableFile, string preparationScript)
        {
            CommandTemplate commandTemplate = unitOfWork.CommandTemplateRepository.GetById(genericCommandTemplateId);
            if (commandTemplate == null)
            {
                throw new RequestedObjectDoesNotExistException("The specified command template is not defined in HEAppE!");
            }

            if (!commandTemplate.IsGeneric)
            {
                throw new InputValidationException("The specified command template is not generic.");
            }

            if (!commandTemplate.IsEnabled)
            {
                throw new InputValidationException("The specified command template is deleted.");
            }

            string scriptPath = commandTemplate.TemplateParameters.Where(w => w.IsVisible)
                                                                    .First()?.Identifier;
            if (string.IsNullOrEmpty(scriptPath))
            {
                throw new RequestedObjectDoesNotExistException("The user-script command parameter for the generic command template is not defined in HEAppE!");
            }

            if (string.IsNullOrEmpty(executableFile))
            {
                throw new InputValidationException("The generic command template should contain script path!");
            }

            Cluster cluster = commandTemplate.ClusterNodeType.Cluster;
            var commandTemplateParameters = new List<string>() { };
            commandTemplateParameters.AddRange(
                SchedulerFactory.GetInstance(cluster.SchedulerType)
                                .CreateScheduler(cluster)
                                .GetParametersFromGenericUserScript(cluster, executableFile)
                                .ToList()
            );

            List<CommandTemplateParameter> templateParameters = new();
            foreach (string parameter in commandTemplateParameters)
            {
                templateParameters.Add(new CommandTemplateParameter()
                {
                    Identifier = parameter,
                    Description = parameter,
                    Query = string.Empty
                }
                );
            }

            CommandTemplate newCommandTemplate = new CommandTemplate()
            {
                Name = name,
                Description = description,
                IsGeneric = false,
                IsEnabled = true,
                ClusterNodeType = commandTemplate.ClusterNodeType,
                ClusterNodeTypeId = commandTemplate.ClusterNodeTypeId,
                Code = code,
                ExecutableFile = executableFile,
                PreparationScript = preparationScript,
                TemplateParameters = templateParameters,
                CommandParameters = string.Join(' ', commandTemplateParameters.Select(x => $"%%{"{"}{x}{"}"}"))
            };

            unitOfWork.CommandTemplateRepository.Insert(newCommandTemplate);
            unitOfWork.Save();


            return newCommandTemplate;

        }

        public CommandTemplate ModifyCommandTemplate(long commandTemplateId, string name, string description, string code, string executableFile, string preparationScript)
        {
            CommandTemplate commandTemplate = unitOfWork.CommandTemplateRepository.GetById(commandTemplateId);
            if (commandTemplate == null)
            {
                throw new RequestedObjectDoesNotExistException("The specified command template is not defined in HEAppE!");
            }

            if (!commandTemplate.IsEnabled)
            {
                throw new InputValidationException("The specified command template is deleted.");
            }

            if (commandTemplate.IsGeneric)
            {
                throw new InputValidationException("The specified command template is generic.");
            }

            if (name != null)
            {
                commandTemplate.Name = name;
            }
            if (description != null)
            {
                commandTemplate.Description = description;
            }
            if (code != null)
            {
                commandTemplate.Code = code;
            }
            if (executableFile != null)
            {
                Cluster cluster = commandTemplate.ClusterNodeType.Cluster;
                var commandTemplateParameters = new List<string>() { };
                commandTemplateParameters.AddRange(
                    SchedulerFactory.GetInstance(cluster.SchedulerType)
                                    .CreateScheduler(cluster)
                                    .GetParametersFromGenericUserScript(cluster, executableFile)
                                    .ToList()
                );

                List<CommandTemplateParameter> templateParameters = new List<CommandTemplateParameter>();
                foreach (string parameter in commandTemplateParameters)
                {
                    templateParameters.Add(new CommandTemplateParameter()
                    {
                        Identifier = parameter,
                        Description = parameter,
                        Query = string.Empty
                    }
                    );
                }

                commandTemplate.TemplateParameters.ForEach(unitOfWork.CommandTemplateParameterRepository.Delete);
                commandTemplate.TemplateParameters.AddRange(templateParameters);
                commandTemplate.ExecutableFile = executableFile;
                commandTemplate.CommandParameters = string.Join(' ', commandTemplateParameters.Select(x => $"%%{"{"}{x}{"}"}"));
            }

            if (preparationScript != null)
            {
                commandTemplate.PreparationScript = preparationScript;
            }

            unitOfWork.Save();
            return commandTemplate;
        }

        public void RemoveCommandTemplate(long commandTemplateId)
        {
            CommandTemplate commandTemplate = unitOfWork.CommandTemplateRepository.GetById(commandTemplateId);
            if (commandTemplate == null)
            {
                throw new RequestedObjectDoesNotExistException("The specified command template is not defined in HEAppE!");
            }
            commandTemplate.IsEnabled = false;
            unitOfWork.Save();
        }

    }
}
