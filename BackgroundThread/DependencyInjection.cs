using HEAppE.BackgroundThread.BackgroundServices;
using Microsoft.Extensions.DependencyInjection;

namespace HEAppE.BackgroundThread
{
    public static class DependencyInjection
    {
        /// <summary>
        /// Register background services into DI container
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public static IServiceCollection AddBackgroundServices(this IServiceCollection services)
        {
            services.AddHostedService<CloseConnectionToFinishedJobsBackgroundService>();
            services.AddHostedService<ClusterAccountRotationJobBackgroundService>();
            services.AddHostedService<RemoveTemporaryFileTransferKeyBackgroundService>();
            services.AddHostedService<UpdateUnfinishedJobsBackgroundService>();
            services.AddHostedService<DatabaseFullBackupBackgroundService>();
            services.AddHostedService<DatabaseTransactionLogBackupService>();

            return services;
        }
    }
}
