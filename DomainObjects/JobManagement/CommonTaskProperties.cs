using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;
using HEAppE.DomainObjects.JobManagement.JobInformation;

namespace HEAppE.DomainObjects.JobManagement {
	public abstract class CommonTaskProperties : IdentifiableDbEntity {
		[Required]
		[StringLength(50)]
		public string Name { get; set; }

		public int? MinCores { get; set; }

		public int? MaxCores { get; set; }

        public TaskPriority? Priority { get; set; }

		[StringLength(50)]
		public string Project { get; set; }

		public int? WalltimeLimit { get; set; }

        // Objects in all related collections have to implement the ICloneable interface to support the combination with client job specification.
        public virtual List<EnvironmentVariable> EnvironmentVariables { get; set; } = new List<EnvironmentVariable>();

		public CommonTaskProperties() : base() { }
		public CommonTaskProperties(CommonTaskProperties commonTaskProperties) : base(commonTaskProperties) 
		{
			this.Name = commonTaskProperties.Name;
			this.MinCores = commonTaskProperties.MinCores;
			this.MaxCores = commonTaskProperties.MaxCores;
			this.Priority = commonTaskProperties.Priority;
			this.Project = commonTaskProperties.Project;
			this.WalltimeLimit = commonTaskProperties.WalltimeLimit;
			//ref types 
			this.EnvironmentVariables = new List<EnvironmentVariable>();
			if(commonTaskProperties.EnvironmentVariables != null)
				foreach (var envVariable in commonTaskProperties.EnvironmentVariables)
					this.EnvironmentVariables.Add(new EnvironmentVariable(envVariable));
		}


		public override string ToString() {
			StringBuilder result = new StringBuilder();
			result.AppendLine("Id=" + Id);
			result.AppendLine("Name=" + Name);
			result.AppendLine("MinCores=" + MinCores);
			result.AppendLine("MaxCores=" + MaxCores);
			result.AppendLine("Priority=" + Priority);
			result.AppendLine("Project=" + Project);
			result.AppendLine("WalltimeLimit=" + WalltimeLimit);
			int i = 0;
			if ( EnvironmentVariables != null )
				foreach (EnvironmentVariable variable in EnvironmentVariables) {
					result.AppendLine("EnvironmentVariable" + (i++) + ": " + variable);
				}
			return result.ToString();
		}
	}
}