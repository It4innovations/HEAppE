using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace HEAppE.DomainObjects.JobManagement
{
    [Table("Project")]
    public class Project : IdentifiableDbEntity
    {
        [Required]
        [StringLength(15)]
        public string AccountingString { get; set; }

        [Required]
        public DateTime StartDate { get; set; }

        [Required]
        public DateTime EndDate { get; set; }

        #region Public methods
        public override string ToString()
        {
            return String.Format($"Project: Id={Id}, AccountingString={AccountingString}, StartDate={StartDate}, EndDate={EndDate}");
        }
        #endregion
    }
}
