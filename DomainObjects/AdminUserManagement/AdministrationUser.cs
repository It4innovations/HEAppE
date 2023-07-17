using HEAppE.DomainObjects.Logging;
using HEAppE.DomainObjects.Notifications;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace HEAppE.DomainObjects.AdminUserManagement
{
    [Table("AdministrationUser")]
    public class AdministrationUser : IdentifiableDbEntity, ILogUserIdentification
    {

        //public virtual AdaptorUser User { get; set; }

        [Required]
        [StringLength(50)]
        public string Email { get; set; }

        [Required]
        [StringLength(30)]
        public string Password { get; set; }

        public DateTime CreationTime { get; set; }

        public DateTime LastModificationTime { get; set; }

        public bool Deleted { get; set; }

        public virtual Language Language { get; set; }

        public virtual List<AdministrationUserRole> AdministrationUserRoles { get; set; } = new List<AdministrationUserRole>();

        [NotMapped]
        public List<AdministrationRole> AdministrationRoles => AdministrationUserRoles.Select(r => r.AdministrationRole).ToList();

        public string GetLogIdentification()
        {
            return Email;
        }
    }
}