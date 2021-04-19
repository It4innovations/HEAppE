﻿using HEAppE.ExtModels.ClusterInformation.Models;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using HEAppE.Utils;

namespace HEAppE.ExtModels.JobManagement.Models
{
    [DataContract(Name = "TaskSpecificationExt")]
    public class TaskSpecificationExt
    {
        [DataMember(Name = "Name")]
        public string Name { get; set; }

        [DataMember(Name = "MinCores")]
        public int? MinCores { get; set; }

        [DataMember(Name = "MaxCores")]
        public int? MaxCores { get; set; }

        [DataMember(Name = "WalltimeLimit")]
        public int? WalltimeLimit { get; set; }

        [DataMember(Name = "PlacementPolicy")]
        public string PlacementPolicy { get; set; }

	    [DataMember(Name = "Priority")]
        public TaskPriorityExt? Priority { get; set; }

        [DataMember(Name = "JobArrays")]
        public string JobArrays { get; set; }

        [DataMember(Name = "IsExclusive")]
        public bool? IsExclusive { get; set; }

        [DataMember(Name = "IsRerunnable")]
        public bool? IsRerunnable { get; set; }

        [DataMember(Name = "StandardInputFile")]
        public string StandardInputFile { get; set; }

        [DataMember(Name = "StandardOutputFile")]
        public string StandardOutputFile { get; set; }

        [DataMember(Name = "StandardErrorFile")]
        public string StandardErrorFile { get; set; }

        [DataMember(Name = "ProgressFile")]
        public string ProgressFile { get; set; }

        [DataMember(Name = "LogFile")]
        public string LogFile { get; set; }

        [DataMember(Name = "ClusterTaskSubdirectory")]
        public string ClusterTaskSubdirectory { get; set; }

        [DataMember(Name = "ClusterNodeTypeId")]
        public long? ClusterNodeTypeId { get; set; }

        [DataMember(Name = "CommandTemplateId")]
        public long? CommandTemplateId { get; set; }

        [DataMember(Name = "CpuHyperThreading")]
        public bool? CpuHyperThreading { get; set; }

        [DataMember(Name = "RequiredNodes")]
        public string[] RequiredNodes { get; set; }

        [DataMember(Name = "TaskParalizationParameter")]
        public TaskParalizationParameterExt[] TaskParalizationParameters { get; set; }

        [DataMember(Name = "EnvironmentVariables")]
        public EnvironmentVariableExt[] EnvironmentVariables { get; set; }

        [DataMember(Name = "DependsOn")]
        public TaskSpecificationExt[] DependsOn { get; set; }

        [DataMember(Name = "TemplateParameterValues")]
        public CommandTemplateParameterValueExt[] TemplateParameterValues { get; set; }
        public override string ToString()
        {
            return $"TaskSpecificationExt(name={Name}; minCores={MinCores}; maxCores={MaxCores}; walltimeLimit={WalltimeLimit}; requiredNodes={RequiredNodes}; priority={Priority}; jobArrays={JobArrays}; isExclusive={IsExclusive}; isRerunnable={IsRerunnable}; standardInputFile={StandardInputFile}; standardOutputFile={StandardOutputFile}; standardErrorFile={StandardErrorFile}; progressFile={ProgressFile}; logFile={LogFile}; clusterTaskSubdirectory={ClusterTaskSubdirectory}; clusterNodeTypeId={ClusterNodeTypeId}; commandTemplateId={CommandTemplateId}; taskParalizationParameters={TaskParalizationParameters}; environmentVariables={EnvironmentVariables}; dependsOn={DependsOn}; templateParameterValues={TemplateParameterValues})";
        }

        public override bool Equals(object obj)
        {
            var x = this.CompareRecursive(obj);
            return x;
        }

        public override int GetHashCode()
        {
            return this.GetObjectHashCodeRecursive();
        }
    }
}