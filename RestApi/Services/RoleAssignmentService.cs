using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HEAppE.BusinessLogicTier.Configuration;
using HEAppE.DataAccessTier.UnitOfWork;
using log4net;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace HEAppE.RestApi.Services;

public class RoleAssignmentService : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private static readonly ILog _log = LogManager.GetLogger(typeof(RoleAssignmentService));

    public RoleAssignmentService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _ = Task.Run(async () => await RunRoleAssignment(), cancellationToken);
        return Task.CompletedTask;
    }

    private async Task RunRoleAssignment()
    {
        using var scope = _serviceProvider.CreateScope();
        var services = scope.ServiceProvider;

        try
        {
            using IUnitOfWork unitOfWork = new DatabaseUnitOfWork();
            _log.Info("Starting post-startup role assignment procedure via IHostedService.");
            
            var userGroups = await unitOfWork.AdaptorUserGroupRepository.GetAllAsync();

            if (userGroups != null)
            {
                foreach (var userGroup in userGroups)
                {
                    RoleAssignmentConfiguration.AssignAllRolesFromConfig(userGroup, unitOfWork, _log);
                }
                _log.Info("Role assignment procedure finished successfully.");
            }
            else
            {
                _log.Warn("Role assignment skipped: No AdaptorUserGroup found in database.");
            }
        }
        catch (Exception ex)
        {
            _log.Error("An error occurred during the role assignment service execution.", ex);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}