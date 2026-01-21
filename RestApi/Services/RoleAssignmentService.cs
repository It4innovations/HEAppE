using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HEAppE.BusinessLogicTier.Configuration;
using HEAppE.DataAccessTier.UnitOfWork;
using HEAppE.DomainObjects.UserAndLimitationManagement;
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
        try
        {
            _log.Info("Starting post-startup role assignment procedure via IHostedService.");
            List<AdaptorUserGroup> userGroups;
            using (IUnitOfWork bootstrapUow = new DatabaseUnitOfWork())
            {
                var groups = await bootstrapUow.AdaptorUserGroupRepository.GetAllAsync();
                userGroups = groups?.ToList() ?? new List<AdaptorUserGroup>();
            }

            if (userGroups.Any())
            {
                foreach (var userGroup in userGroups)
                {
                    // 2. Pro KAŽDOU skupinu vytvoříme nový, čistý UnitOfWork
                    // Tím se vyhneme chybě "already being tracked"
                    using (IUnitOfWork workerUow = new DatabaseUnitOfWork())
                    {
                        _log.Debug($"Processing roles for group: {userGroup.Name}");
                        var localGroup = workerUow.AdaptorUserGroupRepository.GetById(userGroup.Id);
                    
                        if (localGroup != null)
                        {
                            RoleAssignmentConfiguration.AssignAllRolesFromConfig(localGroup, workerUow, _log);
                        }
                    }
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