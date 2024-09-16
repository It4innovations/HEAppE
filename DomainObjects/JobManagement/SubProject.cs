using HEAppE.DomainObjects.JobReporting.Enums;
using HEAppE.DomainObjects.UserAndLimitationManagement;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HEAppE.DomainObjects.JobManagement.JobInformation;

namespace HEAppE.DomainObjects.JobManagement
{
    [Table("SubProject")]
    public class SubProject : IdentifiableDbEntity
    {
        [Required]
        [StringLength(50)]
        public string Identifier { get; set; }

        [StringLength(100)]
        public string Description { get; set; }

        [Required]
        public DateTime StartDate { get; set; }
        
        public DateTime? EndDate { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? ModifiedAt { get; set; }

        [Required]
        public bool IsDeleted { get; set; } = false;
        
        [ForeignKey("Project")]
        public long ProjectId { get; set; }
        public virtual Project Project { get; set; }
        
        public virtual List<JobSpecification> JobSpecifications { get; set; } = new List<JobSpecification>();

        #region Public methods
        public override string ToString()
        {
            return $"SubProject: Id={Id}, Identifier={Identifier}, Description={Description}, StartDate={StartDate}, EndDate={EndDate}, CreatedAt={CreatedAt}, ModifiedAt={ModifiedAt}, IsDeleted={IsDeleted}";
        }
        #endregion
    }
}
