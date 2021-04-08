using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace HEAppE.DomainObjects.JobManagement {
    [Table("JobTemplate")]
    public class JobTemplate : CommonJobProperties {
        public virtual List<PropertyChangeSpecification> PropertyChangeSpecification { get; set; } = new List<PropertyChangeSpecification>();
	}
}