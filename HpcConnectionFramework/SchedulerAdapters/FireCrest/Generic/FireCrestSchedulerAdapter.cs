using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using HEAppE.DomainObjects.ClusterInformation;
using HEAppE.DomainObjects.JobManagement;
using HEAppE.DomainObjects.JobManagement.JobInformation;
using HEAppE.Exceptions.Internal;
using HEAppE.HpcConnectionFramework.SchedulerAdapters.Interfaces;
using HEAppE.HpcConnectionFramework.SystemCommands;
using HEAppE.HpcConnectionFramework.SystemConnectors.SSH;
using HEAppE.HpcConnectionFramework.SystemConnectors.SSH.DTO;
using log4net;

namespace HEAppE.HpcConnectionFramework.SchedulerAdapters.FireCrest.Generic;

/// <summary>
///     FireCrest scheduler adapter for interacting with HPC systems via FireCrest REST API
/// </summary>
internal class FireCrestSchedulerAdapter : ISchedulerAdapter
{
    #region Instances

    /// <summary>
    ///     Convertor reference
    /// </summary>
    protected ISchedulerDataConvertor _convertor;

    /// <summary>
    ///     Commands for operations that require SSH
    /// </summary>
    protected ICommands _commands;

    /// <summary>
    ///     Logger
    /// </summary>
    protected ILog _log;

    /// <summary>
    ///     HTTP client for API calls
    /// </summary>
    protected HttpClient _httpClient;

    /// <summary>
    ///     FireCrest API base URL
    /// </summary>
    protected string _firecrestUrl;

    /// <summary>
    ///     Base directory path for job files
    /// </summary>
    protected string _baseDirectoryPath;

    /// <summary>
    ///     SSH tunnel utility
    /// </summary>
    protected static SshTunnelUtils _sshTunnelUtil = new SshTunnelUtils();

    #endregion

    #region Constructors

    /// <summary>
    ///     Constructor
    /// </summary>
    /// <param name="convertor">Scheduler data convertor</param>
    public FireCrestSchedulerAdapter(ISchedulerDataConvertor convertor)
    {
        _log = LogManager.GetLogger(typeof(FireCrestSchedulerAdapter));
        _convertor = convertor;
        _commands = new LinuxCommands();

        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(60)
        };

        _firecrestUrl = "http://host.docker.internal:8000"; // CHANGE WITH REAL FIRECREST API URL
        _baseDirectoryPath = "/home/fireuser"; // CHANGE WITH REAL BASE DIRECTORY PATH
    }

    #endregion

    #region Private Methods

    /// <summary>
    ///     Get authentication token for FireCrest API using OAuth2 Client Credentials flow
    /// </summary>
    /// <param name="connectorClient">Connector client (not used for OAuth2)</param>
    /// <returns>Authentication token for API calls</returns>
    private string GetAuthToken(object connectorClient)
    {
        try
        {
            _log.Debug("Requesting OAuth2 token for FireCrest API");
            Console.WriteLine(
                $"[DEBUG {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] Requesting OAuth2 token for FireCrest API");

            var tokenEndpoint = "http://host.docker.internal:8080/auth/realms/kcrealm/protocol/openid-connect/token";
            Console.WriteLine($"[DEBUG {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] Token endpoint: {tokenEndpoint}");

            var clientId = "firecrest-test-client";
            var clientSecret = "wZVHVIEd9dkJDh9hMKc6DTvkqXxnDttk";
            Console.WriteLine($"[DEBUG {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] Using client ID: {clientId}");

            var tokenRequestContent = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("grant_type", "client_credentials"),
                new KeyValuePair<string, string>("client_id", clientId),
                new KeyValuePair<string, string>("client_secret", clientSecret)
            });

            using var request = new HttpRequestMessage(HttpMethod.Post, tokenEndpoint);
            request.Content = tokenRequestContent;
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*"));
            request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");

            Console.WriteLine($"[DEBUG {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] Sending token request...");

            var tokenResponse = _httpClient.SendAsync(request).GetAwaiter().GetResult();
            Console.WriteLine(
                $"[DEBUG {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] Token response status code: {tokenResponse.StatusCode}");
            _log.Debug($"Token request response status: {tokenResponse.StatusCode}");

            if (!tokenResponse.IsSuccessStatusCode)
            {
                var errorContent = tokenResponse.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                _log.Error(
                    $"OAuth2 token request failed. Status: {tokenResponse.StatusCode}, Response: {errorContent}");
                Console.WriteLine(
                    $"[ERROR {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] OAuth2 token request failed. Status: {tokenResponse.StatusCode}");
                Console.WriteLine($"[ERROR {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] Error response: {errorContent}");

                throw new SshCommandException(
                    "Failed to obtain OAuth2 token for FireCrest API",
                    errorContent);
            }

            var responseContent = tokenResponse.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            Console.WriteLine(
                $"[DEBUG {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] Raw response: {responseContent.Substring(0, Math.Min(100, responseContent.Length))}...");

            var tokenData = JsonSerializer.Deserialize<JsonElement>(responseContent);

            if (tokenData.TryGetProperty("access_token", out var accessTokenElement))
            {
                var accessToken = accessTokenElement.GetString();
                var tokenPreview = accessToken.Length > 20
                    ? accessToken.Substring(0, 10) + "..." + accessToken.Substring(accessToken.Length - 10)
                    : accessToken;

                _log.Debug("Successfully retrieved OAuth2 token for FireCrest API");
                Console.WriteLine(
                    $"[DEBUG {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] Successfully retrieved OAuth2 token: {tokenPreview}");

                if (tokenData.TryGetProperty("expires_in", out var expiresInElement))
                {
                    var expiresIn = expiresInElement.GetInt32();
                    Console.WriteLine(
                        $"[DEBUG {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] Token expires in: {expiresIn} seconds");
                }

                return accessToken;
            }

            _log.Error("Access token not found in OAuth2 response");
            Console.WriteLine(
                $"[ERROR {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] Access token not found in OAuth2 response");
            Console.WriteLine($"[ERROR {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] Full response: {responseContent}");

            throw new SshCommandException(
                "Invalid OAuth2 response",
                "Access token not found in response");
        }
        catch (HttpRequestException ex)
        {
            _log.Error($"HTTP request failed when retrieving token: {ex.Message}", ex);
            Console.WriteLine($"[ERROR {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] HTTP request failed: {ex.Message}");
            if (ex.InnerException != null)
            {
                Console.WriteLine(
                    $"[ERROR {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] Inner exception: {ex.InnerException.Message}");
            }

            throw new SshCommandException(
                "HTTP request failed when retrieving FireCrest token",
                ex.Message);
        }
        catch (Exception ex) when (!(ex is SshCommandException))
        {
            _log.Error($"Failed to retrieve FireCrest authentication token: {ex.Message}", ex);
            Console.WriteLine($"[ERROR {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] Failed to retrieve token: {ex.Message}");
            Console.WriteLine($"[ERROR {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] Exception type: {ex.GetType().Name}");

            throw new SshCommandException(
                "Failed to retrieve FireCrest authentication token",
                ex.Message);
        }
    }


    public IEnumerable<SubmittedTaskInfo> SubmitJob(object connectorClient, JobSpecification jobSpecification,
        ClusterAuthenticationCredentials credentials)
    {
        try
        {
            var token = GetAuthToken(connectorClient);
            Console.WriteLine(jobSpecification);
            return null;
        }
        //Error here. 
        catch (Exception ex)
        {
            return null;
        }
    }

    public IEnumerable<SubmittedTaskInfo> GetActualTasksInfo(object connectorClient, Cluster cluster,
        IEnumerable<SubmittedTaskInfo> submitedTasksInfo, string key)
    {
        throw new NotImplementedException();
    }

    public void CancelJob(object connectorClient, IEnumerable<SubmittedTaskInfo> submitedTasksInfo, string message)
    {
        throw new NotImplementedException();
    }

    public ClusterNodeUsage GetCurrentClusterNodeUsage(object connectorClient, ClusterNodeType nodeType)
    {
        throw new NotImplementedException();
    }

    public IEnumerable<string> GetAllocatedNodes(object connectorClient, SubmittedTaskInfo taskInfo)
    {
        throw new NotImplementedException();
    }

    public IEnumerable<string> GetParametersFromGenericUserScript(object connectorClient, string userScriptPath)
    {
        throw new NotImplementedException();
    }

    public void AllowDirectFileTransferAccessForUserToJob(object connectorClient, string publicKey,
        SubmittedJobInfo jobInfo)
    {
        throw new NotImplementedException();
    }

    public void RemoveDirectFileTransferAccessForUser(object connectorClient, IEnumerable<string> publicKeys,
        string projectAccountingString)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    ///     Create job directory
    /// </summary>
    public void CreateJobDirectory(object connectorClient, SubmittedJobInfo jobInfo, string localBasePath,
        bool sharedAccountsPoolMode)
    {
        try
        {
            var token = GetAuthToken(connectorClient);
            string clusterName = jobInfo.Specification?.Cluster?.Name;
            string account = jobInfo.Specification?.ClusterUser?.Username ?? "default";
            localBasePath = localBasePath?.TrimEnd('/') ?? string.Empty;

            string jobDirectoryPath;
            if (!string.IsNullOrEmpty(localBasePath))
            {
                jobDirectoryPath = $"{localBasePath}/instance/executions/{account}/{jobInfo.Specification.Id}";
            }
            else
            {
                jobDirectoryPath = $"{_baseDirectoryPath}/{account}/{jobInfo.Specification.Id}";
            }

            jobDirectoryPath = jobDirectoryPath.Replace("\\", "/");

            _log.Info($"Creating job directory at: {jobDirectoryPath}");
            Console.WriteLine(
                $"[INFO {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] Creating job directory at: {jobDirectoryPath}");

            var endpoint = $"{_firecrestUrl}/filesystem/{clusterName}/ops/mkdir";
            var requestBody = new { path = jobDirectoryPath, parent = true };
            var jsonContent = JsonSerializer.Serialize(requestBody);

            Console.WriteLine($"[DEBUG {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] Request body: {jsonContent}");

            CreateDirectory(endpoint, token, jobDirectoryPath, requestBody);

            _log.Info($"Job directory created successfully: {jobDirectoryPath}");
            Console.WriteLine(
                $"[INFO {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] Job directory created successfully: {jobDirectoryPath}");

            foreach (var task in jobInfo.Tasks)
            {
                try
                {
                    var subdirectoryPath = !string.IsNullOrEmpty(task.Specification?.ClusterTaskSubdirectory)
                        ? $"/{task.Specification.ClusterTaskSubdirectory}"
                        : string.Empty;

                    string taskDirectoryPath = $"{jobDirectoryPath}/{task.Specification.Id}{subdirectoryPath}";
                    taskDirectoryPath = taskDirectoryPath.Replace("\\", "/");

                    Console.WriteLine(
                        $"[DEBUG {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] Creating task directory at: {taskDirectoryPath}");

                    var taskRequestBody = new { path = taskDirectoryPath, parent = true };

                    CreateDirectory(endpoint, token, taskDirectoryPath, taskRequestBody);

                    _log.Info($"Task directory created successfully: {taskDirectoryPath}");
                    Console.WriteLine(
                        $"[INFO {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] Task directory created successfully: {taskDirectoryPath}");
                }
                catch (FirecrestApiException ex)
                {
                    _log.Error(
                        $"Failed to create task directory. Status: {ex.StatusCode}, Response: {ex.ResponseContent}");
                    Console.WriteLine(
                        $"[ERROR {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] Failed to create task directory. Status: {ex.StatusCode}, Response: {ex.ResponseContent}");
                }
            }
        }
        catch (FirecrestApiException ex)
        {
            _log.Error($"Failed to create job directory. Status: {ex.StatusCode}, Response: {ex.ResponseContent}", ex);
            Console.WriteLine(
                $"[ERROR {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] Failed to create job directory. Status: {ex.StatusCode}, Response: {ex.ResponseContent}");

            Console.WriteLine(
                $"[WARN {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] Continuing without job directory creation");
            throw;
        }
        catch (Exception ex)
        {
            _log.Error($"Error creating job directory: {ex.Message}", ex);
            Console.WriteLine(
                $"[ERROR {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] Error creating job directory: {ex.Message}");

            if (ex.InnerException != null)
            {
                Console.WriteLine(
                    $"[ERROR {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] Inner exception: {ex.InnerException.Message}");
            }

            Console.WriteLine(
                $"[WARN {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] Continuing without job directory creation");
            throw; 
        }
    }

    private void CreateDirectory(string endpoint, string token, string directoryPath, object requestBody)
    {
        var jsonContent = JsonSerializer.Serialize(requestBody);

        using var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Content = content;

        var response = _httpClient.SendAsync(request).GetAwaiter().GetResult();
        var responseContent = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();

        Console.WriteLine(
            $"[DEBUG {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] Directory creation response: {response.StatusCode}");

        if (!response.IsSuccessStatusCode)
        {
            throw new FirecrestApiException(
                $"Failed to create directory: {directoryPath}",
                response.StatusCode,
                responseContent);
        }
    }

    /// <summary>
    ///     Delete job directory
    /// </summary>
    public bool DeleteJobDirectory(object connectorClient, SubmittedJobInfo jobInfo, string localBasePath)
    {
        try
        {
            var token = GetAuthToken(connectorClient);
            string clusterName = jobInfo.Specification?.Cluster?.Name;
            string account = jobInfo.Specification?.ClusterUser?.Username ?? "default";
            localBasePath = localBasePath?.TrimEnd('/') ?? string.Empty;

            string jobDirectoryPath;
            if (!string.IsNullOrEmpty(localBasePath))
            {
                jobDirectoryPath = $"{localBasePath}/instance/executions/{account}/{jobInfo.Specification.Id}";
            }
            else
            {
                jobDirectoryPath = $"{_baseDirectoryPath}/{account}/{jobInfo.Specification.Id}";
            }

            jobDirectoryPath = jobDirectoryPath.Replace("\\", "/");

            _log.Info($"Deleting job directory at: {jobDirectoryPath}");
            Console.WriteLine(
                $"[INFO {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] Deleting job directory at: {jobDirectoryPath}");

            var endpoint =
                $"{_firecrestUrl}/filesystem/{clusterName}/ops/rm?path={Uri.EscapeDataString(jobDirectoryPath)}";
            Console.WriteLine($"[DEBUG {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] Delete endpoint: {endpoint}");

            DeleteDirectory(endpoint, token, jobDirectoryPath);

            _log.Info($"Job directory deleted successfully: {jobDirectoryPath}");
            Console.WriteLine(
                $"[INFO {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] Job directory deleted successfully: {jobDirectoryPath}");
            return true;
        }
        catch (FirecrestApiException ex)
        {
            _log.Error($"Failed to delete job directory. Status: {ex.StatusCode}, Response: {ex.ResponseContent}", ex);
            Console.WriteLine(
                $"[ERROR {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] Failed to delete job directory. Status: {ex.StatusCode}, Response: {ex.ResponseContent}");
            return false;
        }
        catch (Exception ex)
        {
            _log.Error($"Error deleting job directory: {ex.Message}", ex);
            Console.WriteLine(
                $"[ERROR {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] Error deleting job directory: {ex.Message}");

            if (ex.InnerException != null)
            {
                Console.WriteLine(
                    $"[ERROR {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] Inner exception: {ex.InnerException.Message}");
            }

            return false;
        }
    }

    private void DeleteDirectory(string endpoint, string token, string directoryPath)
    {
        using var request = new HttpRequestMessage(HttpMethod.Delete, endpoint);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = _httpClient.SendAsync(request).GetAwaiter().GetResult();
        var responseContent = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();

        Console.WriteLine(
            $"[DEBUG {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] Directory deletion response: {response.StatusCode}");

        if (!response.IsSuccessStatusCode)
        {
            throw new FirecrestApiException(
                $"Failed to delete directory: {directoryPath}",
                response.StatusCode,
                responseContent);
        }
    }

    public void CopyJobDataToTemp(object connectorClient, SubmittedJobInfo jobInfo, string localBasePath, string hash,
        string path)
    {
        throw new NotImplementedException();
    }

    public void CopyJobDataFromTemp(object connectorClient, SubmittedJobInfo jobInfo, string hash, string localBasePath)
    {
        throw new NotImplementedException();
    }

    public void CreateTunnel(object connectorClient, SubmittedTaskInfo taskInfo, string nodeHost, int nodePort)
    {
        throw new NotImplementedException();
    }

    public void RemoveTunnel(object connectorClient, SubmittedTaskInfo taskInfo)
    {
        throw new NotImplementedException();
    }

    public IEnumerable<TunnelInfo> GetTunnelsInfos(SubmittedTaskInfo taskInfo, string nodeHost)
    {
        throw new NotImplementedException();
    }

    public bool InitializeClusterScriptDirectory(object schedulerConnectionConnection,
        string clusterProjectRootDirectory,
        bool overwriteExistingProjectRootDirectory, string localBasepath, string account, bool isServiceAccount)
    {
        throw new NotImplementedException();
    }

    public bool MoveJobFiles(object schedulerConnectionConnection, SubmittedJobInfo jobInfo,
        IEnumerable<Tuple<string, string>> sourceDestinations)
    {
        throw new NotImplementedException();
    }

    #endregion
}