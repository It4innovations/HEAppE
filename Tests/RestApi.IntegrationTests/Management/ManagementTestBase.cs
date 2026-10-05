using System.Threading.Tasks;
using HEAppE.ExtModels.UserAndLimitationManagement.Models;
using HEAppE.RestApi.IntegrationTests.Infrastructure;
using HEAppE.RestApiModels.UserAndLimitationManagement;
using Xunit;

namespace HEAppE.RestApi.IntegrationTests.Management;

public abstract class ManagementTestBase : IntegrationTestBase
{
    protected ManagementTestBase(HEAppEWebApplicationFactory factory) : base(factory)
    {
    }
}
