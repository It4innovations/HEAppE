using HEAppE.DomainObjects.OpenStack;

namespace HEAppE.DataAccessTier.IRepository.OpenStack
{
    public interface IOpenStackAuthenticationCredentialsRepository : IRepository<OpenStackAuthenticationCredentials>
    {
        OpenStackAuthenticationCredentials GetDefaultAccount();
    }
}