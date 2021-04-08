using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HEAppE.DomainObjects.JobManagement {
	[Table("PropertyChangeSpecification")]
	public class PropertyChangeSpecification : IdentifiableDbEntity {
		[Required]
		[StringLength(30)]
		public string PropertyName { get; set; }

		public PropertyChangeMethod ChangeMethod { get; set; }

        [ForeignKey("JobTemplate")]
        public long? JobTemplateId { get; set; }
        public virtual JobTemplate JobTemplate { get; set; }

        [ForeignKey("TaskTemplate")]
        public long? TaskTemplateId { get; set; }
        public virtual TaskTemplate TaskTemplate { get; set; }
    }
}