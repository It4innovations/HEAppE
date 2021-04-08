using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace HEAppE.DomainObjects.AdminUserManagement
{
	[Table("AdministrationRole")]
	public class AdministrationRole : IdentifiableDbEntity {
		[Required]
		[StringLength(50)]
		public string Name { get; set; }

		[Required]
		[StringLength(200)]
		public string Description { get; set; }

		[Required]
		[StringLength(10)]
		public string AccessCode { get; set; }

        public virtual List<AdministrationUserRole> AdministrationUserRoles { get; set; } = new List<AdministrationUserRole>();

		[NotMapped]
		public List<AdministrationUser> AdministrationUsers => AdministrationUserRoles.Select(r => r.AdministrationUser).ToList();
	}
}