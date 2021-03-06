using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;
using HEAppE.DomainObjects.JobManagement.JobInformation;

namespace HEAppE.DomainObjects.JobManagement {
	public abstract class CommonJobProperties : IdentifiableDbEntity {
		[Required]
		[StringLength(50)]
		public string Name { get; set; }

		[StringLength(50)]
		public string Project { get; set; }

		public int? WalltimeLimit { get; set; }

        // Objects in all related collections have to implement the ICloneable interface to support the combination with client job specification.
        public virtual List<EnvironmentVariable> EnvironmentVariables { get; set; } = new List<EnvironmentVariable>();

		public override string ToString() {
			StringBuilder result = new StringBuilder();
			result.AppendLine("Id=" + Id);
			result.AppendLine("Name=" + Name);
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