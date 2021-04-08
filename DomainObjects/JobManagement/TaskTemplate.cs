using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace HEAppE.DomainObjects.JobManagement {
    [Table("TaskTemplate")]
    public class TaskTemplate : CommonTaskProperties {
        public virtual List<PropertyChangeSpecification> PropertyChangeSpecification { get; set; } = new List<PropertyChangeSpecification>();
    }
}