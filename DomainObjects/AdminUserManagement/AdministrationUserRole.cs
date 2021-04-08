namespace HEAppE.DomainObjects.AdminUserManagement
{
    public class AdministrationUserRole
    {
        public long AdministrationUserId { get; set; }
        public virtual AdministrationUser AdministrationUser { get; set; }

        public long AdministrationRoleId { get; set; }
        public virtual AdministrationRole AdministrationRole { get; set; }
    }
}
