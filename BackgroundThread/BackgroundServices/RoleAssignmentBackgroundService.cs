using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HEAppE.BackgroundThread.Configuration;
using HEAppE.BusinessLogicTier.Configuration;
using HEAppE.DataAccessTier.UnitOfWork;
using HEAppE.DomainObjects.UserAndLimitationManagement;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace HEAppE.BackgroundThread.BackgroundServices;

public class RoleAssignmentBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger _logger;
    private readonly BackGroundThreadConfiguration _configuration;

    public RoleAssignmentBackgroundService(IServiceScopeFactory scopeFactory, ILoggerFactory loggerFactory, BackGroundThreadConfiguration configuration)
    {
        _scopeFactory = scopeFactory;
        _logger = loggerFactory.CreateLogger("HEAppE.BackgroundThread.BackgroundServices.RoleAssignmentBackgroundService");
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Yield();
        try
        {
            _logger.LogInformation("Starting system role assignment synchronization.");
            
            using (IServiceScope scope = _scopeFactory.CreateScope())
            {
                using (IUnitOfWork workerUow = new DatabaseUnitOfWork(_logger))
                {
                    var groups = await workerUow.AdaptorUserGroupRepository.GetAllAsync();
                    var userGroups = groups?.ToList() ?? new List<AdaptorUserGroup>();

                    _logger.LogInformation($"Syncing roles for {userGroups.Count} user groups.");
                    RoleAssignmentConfiguration.AssignAllRolesFromConfigToAllGroups(userGroups, workerUow, _logger);
                }
            }
            _logger.LogInformation("Role assignment synchronization finished successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Role assignment failed.");
        }
    }
}