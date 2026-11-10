using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HEAppE.ConnectionPool;
using HEAppE.DomainObjects.ClusterInformation;
using HEAppE.DomainObjects.JobManagement;
using HEAppE.DomainObjects.JobManagement.JobInformation;
using HEAppE.DomainObjects.UserAndLimitationManagement;
using HEAppE.HpcConnectionFramework.SchedulerAdapters;
using HEAppE.HpcConnectionFramework.SchedulerAdapters.QScheduler.DTO;
using HEAppE.Utils;
using Microsoft.Extensions.Logging;

using HEAppE.DataAccessTier.UnitOfWork;
using HEAppE.DataAccessTier.Vault;

namespace HEAppE.BusinessLogicTier.Logic.JobManagement.Callbacks;

internal class QSchedulerCallbackHandler : ISchedulerCallbackHandler
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger _logger;
    private readonly JobManagementLogic _logic;

    internal QSchedulerCallbackHandler(IUnitOfWork unitOfWork, ILogger logger, JobManagementLogic logic)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _logic = logic;
    }

    public async Task<long> ProcessSessionCallbackAsync(string scheduledJobId, string token, string? qSchedulerState, string? rawResponse = null)
    {
        if (!long.TryParse(scheduledJobId.Substring("session:".Length), out var sessionId))
        {
            return 0L;
        }

        // Session IDs are unique only per QScheduler instance. If several clusters track the same ID,
        // the callback token tells which cluster (QScheduler instance) sent it.
        var candidateSessions = (await _unitOfWork.QSchedulerSessionRepository.GetAllBySessionIdAsync(sessionId)).ToList();
        if (candidateSessions.Count == 0)
        {
            return 0L;
        }

        QSchedulerSession? dbSession = null;
        foreach (var candidate in candidateSessions)
        {
            var candidateCluster = await _unitOfWork.ClusterRepository.GetByIdAsync(candidate.ClusterId);
            if (candidateCluster != null && await IsValidClusterTokenAsync(candidateCluster, token))
            {
                dbSession = candidate;
                break;
            }
        }

        if (dbSession == null)
        {
            throw new UnauthorizedAccessException("Authentication failed: Invalid callback token.");
        }

        // Token is valid! Update the session state!
        var oldState = dbSession.State;
        var newState = oldState;
        if (string.Equals(qSchedulerState, "open", StringComparison.OrdinalIgnoreCase) || 
            string.Equals(qSchedulerState, "opened", StringComparison.OrdinalIgnoreCase))
        {
            newState = QSchedulerSessionState.Open;
        }
        else if (string.Equals(qSchedulerState, "closed", StringComparison.OrdinalIgnoreCase))
        {
            newState = QSchedulerSessionState.Closed;
            dbSession.ClosedAt ??= DateTime.UtcNow;
        }
        else if (string.Equals(qSchedulerState, "waiting", StringComparison.OrdinalIgnoreCase))
        {
            newState = QSchedulerSessionState.Waiting;
        }

        // The callback carries the session's consumption (exectime_ms) and timestamps.
        var consumptionChanged = JobManagementLogic.ApplyQSchedulerSessionConsumption(dbSession, QSchedulerSessionInfo.Parse(rawResponse));
        if (dbSession.State == QSchedulerSessionState.Closed)
        {
            // A closed session never reopens, whatever the event state says.
            newState = QSchedulerSessionState.Closed;
        }

        if (oldState != newState || consumptionChanged)
        {
            dbSession.State = newState;
            await _unitOfWork.SaveAsync();

            if (oldState != newState)
            {
                var stateStr = newState == QSchedulerSessionState.Open ? "Open" : 
                               newState == QSchedulerSessionState.Closed ? "Closed" : "Waiting";

                await _logic.PublishEventAsync(dbSession.UserId, "org.heappe.session.state-changed", "/heappe/sessions", new
                {
                    sessionId = scheduledJobId,
                    state = stateStr
                });
            }
        }
        return 0L;
    }

    /// <summary>
    ///     Checks the callback token against the cluster's QScheduler notify token (or the master callback token).
    /// </summary>
    private async Task<bool> IsValidClusterTokenAsync(Cluster cluster, string token)
    {
        bool storeInVault = cluster.CustomConfigurationVaultToggles != null &&
                            cluster.CustomConfigurationVaultToggles.TryGetValue("QSchedulerNotifyToken", out bool toggledValue) &&
                            toggledValue;

        string? expectedToken = null;
        if (storeInVault)
        {
            var vaultConnector = new VaultConnector(_logger);
            expectedToken = await vaultConnector.GetClusterSecretAsync(cluster.Id, "QSchedulerNotifyToken");
        }
        else
        {
            cluster.CustomConfiguration?.TryGetValue("QSchedulerNotifyToken", out expectedToken);
        }

        if (string.IsNullOrEmpty(expectedToken))
        {
            expectedToken = await _logic.GetMasterCallbackTokenAsync(cluster);
        }

        return !string.IsNullOrEmpty(expectedToken) && expectedToken == token;
    }

    public async Task<bool> AuthenticateTaskCallbackAsync(string token, SubmittedTaskInfo candidate, SubmittedJobInfo jobInfo, Cluster cluster)
    {
        bool toggled = cluster.CustomConfigurationVaultToggles != null &&
                       cluster.CustomConfigurationVaultToggles.TryGetValue("QSchedulerNotifyToken", out bool toggledValue) &&
                       toggledValue;
        bool storeInVault = toggled;

        string? expectedToken = null;
        if (storeInVault)
        {
            var vaultConnector = new VaultConnector(_logger);
            expectedToken = await vaultConnector.GetClusterSecretAsync(cluster.Id, "QSchedulerNotifyToken");
        }
        else
        {
            if (cluster.CustomConfiguration != null)
            {
                cluster.CustomConfiguration.TryGetValue("QSchedulerNotifyToken", out expectedToken);
            }
        }

        if (string.IsNullOrEmpty(expectedToken))
        {
            expectedToken = await _logic.GetMasterCallbackTokenAsync(cluster);
        }

        if (!string.IsNullOrEmpty(expectedToken) && expectedToken == token)
        {
            return true;
        }

        return false;
    }

    public async Task<CallbackProcessResult> ProcessTaskCallbackAsync(
        string? rawResponse,
        string? qSchedulerState,
        SubmittedTaskInfo dbTask,
        SubmittedJobInfo jobInfo,
        Cluster cluster)
    {
        var result = new CallbackProcessResult { Handled = false };

        if (dbTask.ScheduledJobId.StartsWith("session:"))
        {
            if (string.Equals(qSchedulerState, "open", StringComparison.OrdinalIgnoreCase) || 
                string.Equals(qSchedulerState, "opened", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogInformation($"Callback event 'open/opened' received for QScheduler session '{dbTask.ScheduledJobId}'. Triggering task submission.");
                
                var previousTaskStates = jobInfo.Tasks.ToDictionary(t => t.Id, t => t.State);
                var previousJobState = jobInfo.State;

                ClusterAuthenticationCredentials credentials = null;
                var isQSchedulerHttp = cluster.SchedulerType == SchedulerType.QScheduler && 
                    (cluster.ConnectionProtocol == ClusterConnectionProtocol.Http || cluster.ConnectionProtocol == ClusterConnectionProtocol.Https);

                if (!isQSchedulerHttp)
                {
                    if (jobInfo.Specification.ClusterUser?.AuthenticationType == ClusterAuthenticationCredentialsAuthType.Kerberos)
                    {
                        credentials = jobInfo.Specification.ClusterUser;
                    }
                    else
                    {
                        credentials = await _unitOfWork.ClusterAuthenticationCredentialsRepository.GetServiceAccountCredentials(
                            jobInfo.Specification.ClusterId, jobInfo.Specification.ProjectId, requireIsInitialized: true, adaptorUserId: dbTask.Specification.JobSpecification.Submitter.Id, _logger);
                    }
                }

                var scheduler = SchedulerFactory.GetInstance(cluster.SchedulerType)
                    .CreateScheduler(cluster, jobInfo.Project, _logic.SshCertificateAuthorityService, dbTask.Specification.JobSpecification.Submitter.Id, _logic.ExpirioService, _logic.ExpirioToken, _logger);
                
                var tasksToUpdate = jobInfo.Tasks.Where(t => t.ScheduledJobId == dbTask.ScheduledJobId).ToList();
                tasksToUpdate.ForEach(t => t.ForceSessionSubmit = true);
                var updatedTasks = await scheduler.GetActualTasksInfoAsync(tasksToUpdate, credentials, _logic.HttpContextKeys.Context.SshCaToken, _logic.ExpirioToken);
                
                foreach (var updated in updatedTasks)
                {
                    var t = jobInfo.Tasks.FirstOrDefault(x => x.Id == updated.Id);
                    if (t != null)
                    {
                        t.ScheduledJobId = updated.ScheduledJobId;
                        t.State = updated.State;
                        t.ErrorMessage = updated.ErrorMessage;
                    }
                }
                
                // Update session state in DB to Open
                if (long.TryParse(dbTask.ScheduledJobId.Substring("session:".Length), out var sessionId))
                {
                    var dbSession = await _unitOfWork.QSchedulerSessionRepository.GetBySessionIdAsync(sessionId, cluster.Id);
                    if (dbSession != null && dbSession.State != QSchedulerSessionState.Open)
                    {
                        dbSession.State = QSchedulerSessionState.Open;
                    }
                    JobManagementLogic.ApplyQSchedulerSessionConsumption(dbSession, QSchedulerSessionInfo.Parse(rawResponse));
                }
                
                JobManagementLogic.UpdateJobStateByTasks(jobInfo);
                await _unitOfWork.SaveAsync();
                JobCacheManager.InvalidateJobCache(jobInfo.Id);
                await _logic.CheckAndCloseQSchedulerSessionsAsync(jobInfo);

                await _logic.PublishEventAsync(jobInfo.Submitter.Id, "org.heappe.session.state-changed", "/heappe/sessions", new
                {
                    sessionId = dbTask.ScheduledJobId,
                    state = "Open"
                });

                await _logic.PublishStateChangesAsync(jobInfo, previousTaskStates, previousJobState);

                result.Handled = true;
                result.JobId = jobInfo.Id;
                return result;
            }
            else if (string.Equals(qSchedulerState, "closed", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogInformation($"Callback event 'closed' received for QScheduler session '{dbTask.ScheduledJobId}'. Closing session and failing unfinished tasks.");
                
                var previousTaskStates = jobInfo.Tasks.ToDictionary(t => t.Id, t => t.State);
                var previousJobState = jobInfo.State;

                // Update session state in DB to Closed
                if (long.TryParse(dbTask.ScheduledJobId.Substring("session:".Length), out var sessionId))
                {
                    var dbSession = await _unitOfWork.QSchedulerSessionRepository.GetBySessionIdAsync(sessionId, cluster.Id);
                    if (dbSession != null && dbSession.State != QSchedulerSessionState.Closed)
                    {
                        dbSession.State = QSchedulerSessionState.Closed;
                        dbSession.ClosedAt = DateTime.UtcNow;
                    }
                    // The close event carries the final consumption; QScheduler's closed_at replaces ours if present.
                    JobManagementLogic.ApplyQSchedulerSessionConsumption(dbSession, QSchedulerSessionInfo.Parse(rawResponse));
                }

                // Fail all candidate tasks that belong to this session but haven't finished yet
                var sessionTasks = jobInfo.Tasks.Where(t => t.ScheduledJobId.StartsWith(dbTask.ScheduledJobId)).ToList();
                foreach (var st in sessionTasks)
                {
                    if (st.State < TaskState.Finished)
                    {
                        st.State = TaskState.Failed;
                        st.ErrorMessage = "Session closed in QScheduler.";
                        HEAppE.Utils.QSchedulerPayloadContext.RemoveByTaskId(st.Id);
                    }
                }
                
                JobManagementLogic.UpdateJobStateByTasks(jobInfo);
                await _unitOfWork.SaveAsync();
                JobCacheManager.InvalidateJobCache(jobInfo.Id);

                await _logic.PublishEventAsync(jobInfo.Submitter.Id, "org.heappe.session.state-changed", "/heappe/sessions", new
                {
                    sessionId = dbTask.ScheduledJobId,
                    state = "Closed"
                });

                await _logic.PublishStateChangesAsync(jobInfo, previousTaskStates, previousJobState);

                result.Handled = true;
                result.JobId = jobInfo.Id;
                return result;
            }
            else if (string.Equals(qSchedulerState, "waiting", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogInformation($"Callback event 'waiting' received for QScheduler session '{dbTask.ScheduledJobId}'. Updating DB to Waiting state.");
                
                // Update session state in DB to Waiting
                if (long.TryParse(dbTask.ScheduledJobId.Substring("session:".Length), out var sessionId))
                {
                    var dbSession = await _unitOfWork.QSchedulerSessionRepository.GetBySessionIdAsync(sessionId, cluster.Id);
                    if (dbSession != null && dbSession.State != QSchedulerSessionState.Waiting)
                    {
                        dbSession.State = QSchedulerSessionState.Waiting;
                        await _unitOfWork.SaveAsync();
                        JobCacheManager.InvalidateJobCache(jobInfo.Id);
                    }
                }

                await _logic.PublishEventAsync(jobInfo.Submitter.Id, "org.heappe.session.state-changed", "/heappe/sessions", new
                {
                    sessionId = dbTask.ScheduledJobId,
                    state = "Waiting"
                });

                result.Handled = true;
                result.JobId = jobInfo.Id;
                return result;
            }
        }

        // Standard task status callback
        var targetState = TaskState.Unknown;
        if (!string.IsNullOrEmpty(qSchedulerState))
        {
            targetState = qSchedulerState.ToLower() switch
            {
                "waiting" => TaskState.Queued,
                "running" => TaskState.Running,
                "started" => TaskState.Running,
                "finished" => TaskState.Finished,
                "failed" => TaskState.Failed,
                "error" => TaskState.Failed,
                "cancelled" => TaskState.Canceled,
                _ => TaskState.Unknown
            };
        }

        string? errorMessage = null;
        string? reason = null;
        string? allParameters = null;
        double? allocatedTime = null;
        DateTime? startTime = null;
        DateTime? endTime = null;

        if (!string.IsNullOrEmpty(rawResponse))
        {
            var convertor = SchedulerFactory.GetInstance(cluster.SchedulerType).GetDataConvertor(_logger);
            var parsedTasks = convertor.ReadParametersFromResponse(cluster, rawResponse);
            var parsedTaskInfo = parsedTasks?.FirstOrDefault();

            if (parsedTaskInfo != null)
            {
                if (targetState == TaskState.Unknown)
                {
                    targetState = parsedTaskInfo.State;
                }
                errorMessage = parsedTaskInfo.ErrorMessage;
                reason = parsedTaskInfo.Reason;
                allParameters = parsedTaskInfo.AllParameters;
                allocatedTime = parsedTaskInfo.AllocatedTime;
                startTime = parsedTaskInfo.StartTime;
                endTime = parsedTaskInfo.EndTime;
            }
        }
        else if (targetState == TaskState.Unknown)
        {
            _logger.LogWarning($"ProcessTaskCallbackAsync: Both rawResponse and qSchedulerState are missing or could not be mapped. Skipping status update.");
        }

        result.TargetState = targetState;
        result.ErrorMessage = errorMessage;
        result.Reason = reason;
        result.AllParameters = allParameters;
        result.AllocatedTime = allocatedTime;
        result.StartTime = startTime;
        result.EndTime = endTime;

        return result;
    }


    public async Task PostProcessCallbackAsync(SubmittedTaskInfo dbTask, SubmittedJobInfo jobInfo, Cluster cluster)
    {
        await _logic.CheckAndCloseQSchedulerSessionsAsync(jobInfo);
    }
}
