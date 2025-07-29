using System.Threading.Tasks;
using HEAppE.BusinessLogicTier.Factory;
using HEAppE.DataAccessTier.Factory.UnitOfWork;
using HEAppE.DomainObjects.UserAndLimitationManagement;
using HEAppE.DomainObjects.UserAndLimitationManagement.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace HEAppE.RestApi.Authentication;

public static class HttpContextKeys
{
    public static AdaptorUser AdaptorUser;

    public static async Task<AdaptorUser> Authorize(string token)
    {
        using (var unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
        {
            var userLogic = LogicFactory.GetLogicFactory().CreateUserAndLimitationManagementLogic(unitOfWork);

            var user = await userLogic.HandleTokenAsApiKeyAuthenticationAsync(new LexisCredentials
            {
                OpenIdLexisAccessToken = token
            });

            AdaptorUser = user;
            return user;
        }
    }
}
