using HEAppE.BackgroundThread.BackgroundServices;
using HEAppE.BackgroundThread.Configuration;
using HEAppE.DataAccessTier.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HEAppE.BackgroundThread
{
    public static class DependencyInjection
    {
        /// <summary>
        /// Register background services into DI container
        /// </summary>
        /// <param name="services"></param>
        /// <param name="configuration"></param>
        /// <returns></returns>
        public static IServiceCollection AddBackgroundServices(this IServiceCollection services, IConfiguration configuration)
        {
            configuration.GetSection("BackGroundThreadSettings").Bind(typeof(BackGroundThreadConfiguration));
            configuration.GetSection("DatabaseFullBackupSettings").Bind(typeof(DatabaseFullBackupConfiguration));
            configuration.GetSection("DatabaseTransactionLogBackupSettings").Bind(typeof(DatabaseTransactionLogBackupConfiguration));

            services.AddHostedService<RoleAssignmentBackgroundService>();
            services.AddHostedService<CloseConnectionToFinishedJobsBackgroundService>();
            services.AddHostedService<ClusterAccountRotationJobBackgroundService>();
            services.AddHostedService<RemoveTemporaryFileTransferKeyBackgroundService>();
            services.AddHostedService<UpdateUnfinishedJobsBackgroundService>();
            services.AddHostedService<DatabaseFullBackupBackgroundService>();
            services.AddHostedService<DatabaseTransactionLogBackupService>();
            services.AddHostedService<ClusterProjectCredentialsCheckLogBackgroundService>();

            return services;
        }
    }
}
