using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HEAppE.BackgroundThread.Configuration;
using HEAppE.BusinessLogicTier.Configuration;
using HEAppE.DataAccessTier.UnitOfWork;
using HEAppE.DomainObjects.UserAndLimitationManagement;
using log4net;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace HEAppE.BackgroundThread.BackgroundServices;

public class RoleAssignmentBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private static readonly ILog _log = LogManager.GetLogger(typeof(RoleAssignmentBackgroundService));
    private readonly TimeSpan _interval = TimeSpan.FromSeconds(BackGroundThreadConfiguration.RoleAssignmentSyncCheck);

    public RoleAssignmentBackgroundService(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Yield();
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _log.Info("Starting system role assignment synchronization.");
                
                using (IServiceScope scope = _scopeFactory.CreateScope())
                {
                    using (IUnitOfWork bootstrapUow = new DatabaseUnitOfWork())
                    {
                        var groups = await bootstrapUow.AdaptorUserGroupRepository.GetAllAsync();
                        var userGroups = groups?.ToList() ?? new List<AdaptorUserGroup>();

                        foreach (var userGroup in userGroups)
                        {
                            if (stoppingToken.IsCancellationRequested) break;

                            using (IUnitOfWork workerUow = new DatabaseUnitOfWork())
                            {
                                var localGroup = workerUow.AdaptorUserGroupRepository.GetById(userGroup.Id);
                                if (localGroup != null)
                                {
                                    RoleAssignmentConfiguration.AssignAllRolesFromConfig(localGroup, workerUow, _log);
                                }
                            }
                        }
                    }
                }
                _log.Info("Role assignment synchronization finished successfully.");
            }
            catch (Exception ex)
            {
                _log.Error("Role assignment failed, will retry in next interval.", ex);
            }

            try
            {
                await Task.Delay(_interval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }
}