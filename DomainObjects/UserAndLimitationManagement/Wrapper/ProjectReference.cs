using HEAppE.DomainObjects.JobManagement;

namespace HEAppE.DomainObjects.UserAndLimitationManagement.Wrapper
{
    public class ProjectReference
    {
        public Project Project { get; set; }
        public AdaptorUserRole Role { get; set; }
    }
}
