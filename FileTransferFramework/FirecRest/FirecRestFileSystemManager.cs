using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using HEAppE.DomainObjects.ClusterInformation;
using HEAppE.DomainObjects.FileTransfer;
using HEAppE.DomainObjects.JobManagement;
using HEAppE.DomainObjects.JobManagement.JobInformation;
using HEAppE.Exceptions.Internal;
using HEAppE.Services.Expirio;
using HEAppE.Services.FirecRest;
using HEAppE.HpcConnectionFramework.Configuration;
using HEAppE.Utils;

namespace HEAppE.FileTransferFramework.FirecRest;

public class FirecRestFileSystemManager : AbstractFileSystemManager
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IExpirioService _expirio;
    private readonly IFirecRestTokenService _tokenService;
    private HttpClient _httpClient => _httpClientFactory.CreateClient("FirecREST");

    public FirecRestFileSystemManager(ILogger logger, FileTransferMethod configuration,
        FileSystemFactory synchronizerFactory, IHttpClientFactory httpClientFactory, IExpirioService expirio, IFirecRestTokenService tokenService)
        : base(logger, configuration, synchronizerFactory)
    {
        _httpClientFactory = httpClientFactory;
        _expirio = expirio;
        _tokenService = tokenService;
    }

    private async Task<(string Url, string Token)> GetFirecrestUrlAndTokenAsync(Cluster cluster, string userToken)
    {
        string url = FirecRestUtils.GetFirecRestUrl(cluster);
        string idpUrl = "";

        if (cluster.CustomConfiguration != null && cluster.CustomConfiguration.TryGetValue("IdpUrl", out var customIdpUrl))
        {
            idpUrl = customIdpUrl;
        }

        string clientId = "";
        string clientSecret = "";

        var options = await _expirio.ExchangeFirecrestCredentialsAsync(userToken, cluster.CustomConfiguration, _logger);
        if (options != null)
        {
            if (options.TryGetValue("clientId", out var value))
                clientId = value;
            if (options.TryGetValue("clientSecret", out value))
                clientSecret = value;
        }

        if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
        {
            throw new FirecRestException("FirecRest credentials must be obtained from Expirio. Direct access via Proxy Connection is not allowed.")
            {
                CommandError = "Missing Expirio credentials"
            };
        }

        var token = await _tokenService.GetTokenAsync(clientId, clientSecret, idpUrl);
        return (url.TrimEnd('/'), token);
    }

    private async Task<ICollection<FileInformation>> ListChangedFilesInDirectoryAsync(
        string firecRestUrl, string token, string systemName, string rootDirectory, string currentDirectory, DateTime? lastModificationLimit)
    {
        var results = new List<FileInformation>();
        var endpoint = $"{firecRestUrl}/filesystem/{systemName}/ops/ls?path={Uri.EscapeDataString(currentDirectory.Replace("\\", "/"))}";

        _logger.LogDebug($"[FirecRest LS] GET {endpoint}");

        using var request = new HttpRequestMessage(HttpMethod.Get, endpoint);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _httpClient.SendAsync(request);
        var responseContent = await response.Content.ReadAsStringAsync();
        _logger.LogDebug($"[FirecRest LS Response] Status: {response.StatusCode}, Content: {responseContent}");

        if (!response.IsSuccessStatusCode)
        {
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound || 
                (responseContent != null && (responseContent.Contains("No such file or directory") || responseContent.Contains("exit status:2"))))
            {
                _logger.LogInformation($"Directory {currentDirectory} does not exist on cluster {systemName}. Returning empty file list.");
                return results;
            }

            _logger.LogError($"Failed to list files in directory {currentDirectory} on cluster {systemName}. Status: {response.StatusCode}, Response: {responseContent}");
            throw new FirecrestApiException($"Failed to list files. Status: {response.StatusCode}, Response: {responseContent}", response.StatusCode, responseContent);
        }

        var files = FirecRestUtils.ParseLsResponse(responseContent);
        if (files != null)
        {
            foreach (var element in files)
            {
                if (element.Name == "." || element.Name == "..") continue;

                string fullPath = $"{currentDirectory.TrimEnd('/')}/{element.Name}";

                if (element.Type == "d")
                {
                    results.AddRange(await ListChangedFilesInDirectoryAsync(firecRestUrl, token, systemName, rootDirectory, fullPath, lastModificationLimit));
                }
                else
                {
                    DateTime lastModifiedDate = DateTime.MinValue;
                    if (!string.IsNullOrEmpty(element.LastModified))
                    {
                        DateTime.TryParse(element.LastModified, out lastModifiedDate);
                    }

                    if (!lastModificationLimit.HasValue || lastModificationLimit.Value <= lastModifiedDate)
                    {
                        string relativeFileName = fullPath;
                        int index = fullPath.IndexOf(rootDirectory, StringComparison.Ordinal);
                        if (index != -1)
                        {
                            relativeFileName = fullPath.Substring(index + rootDirectory.Length).TrimStart('/');
                        }

                        results.Add(new FileInformation
                        {
                            FileName = relativeFileName,
                            LastModifiedDate = lastModifiedDate
                        });
                    }
                }
            }
        }

        return results;
    }

    #region AbstractFileSystemManager Members

    public override async Task<byte[]> DownloadFileFromClusterAsync(SubmittedJobInfo jobInfo, string relativeFilePath, string sshCaToken, string lexisToken)
    {
        var basePath = jobInfo.Specification.Cluster.ClusterProjects
            .Find(cp => cp.ProjectId == jobInfo.Specification.ProjectId)?.ScratchStoragePath;
        if (string.IsNullOrEmpty(basePath))
        {
            basePath = jobInfo.Specification.Cluster.ClusterProjects
                .Find(cp => cp.ProjectId == jobInfo.Specification.ProjectId)?.ProjectStoragePath;
        }

        var clusterConfig = ClusterRuntimeConfiguration.For(jobInfo.Specification.Cluster.CustomConfiguration);
        var localBasePath = Path.Combine(basePath, clusterConfig.SubExecutionsPath.TrimStart('/'));
        var partPath = localBasePath.Replace(basePath, string.Empty);
        var file = Path.Combine(basePath, clusterConfig.InstanceIdentifierPath, partPath.TrimStart('/'), jobInfo.Specification.ClusterUser.Username, relativeFilePath.TrimStart('/'));

        return await DownloadFileFromClusterByAbsolutePathAsync(jobInfo.Specification, file, sshCaToken, lexisToken);
    }

    public override async Task<byte[]> DownloadFileFromClusterByAbsolutePathAsync(JobSpecification jobSpecification, string absoluteFilePath, string sshCaToken, string lexisToken)
    {
        var (firecRestUrl, token) = await GetFirecrestUrlAndTokenAsync(jobSpecification.Cluster, lexisToken);
        string systemName = jobSpecification.Cluster.Name;

        string username = jobSpecification.ClusterUser?.Username ?? string.Empty;
        string expandedPath = FirecRestUtils.ExpandRemotePath(absoluteFilePath, username, jobSpecification.Cluster.CustomConfiguration);

        var endpoint = $"{firecRestUrl}/filesystem/{systemName}/ops/view?path={Uri.EscapeDataString(expandedPath.Replace("\\", "/"))}";
        _logger.LogDebug($"[FirecRest View] GET {endpoint}");

        using var request = new HttpRequestMessage(HttpMethod.Get, endpoint);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _httpClient.SendAsync(request);
        _logger.LogDebug($"[FirecRest View Response] Status: {response.StatusCode}");
        var responseContent = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError($"Failed to download file {expandedPath} from cluster {systemName}. Status: {response.StatusCode}, Response: {responseContent}");
            throw new FirecrestApiException($"Failed to download file. Status: {response.StatusCode}, Response: {responseContent}", response.StatusCode, responseContent);
        }

        string contentStr = FirecRestUtils.ParseFileContent(responseContent);
        return System.Text.Encoding.UTF8.GetBytes(contentStr ?? string.Empty);
    }

    public override async Task DeleteSessionFromClusterAsync(SubmittedJobInfo jobInfo, string sshCaToken, string lexisToken)
    {
        var (firecRestUrl, token) = await GetFirecrestUrlAndTokenAsync(jobInfo.Specification.Cluster, lexisToken);
        string systemName = jobInfo.Specification.Cluster.Name;

        var clusterConfig = ClusterRuntimeConfiguration.For(jobInfo.Specification.Cluster.CustomConfiguration);
        string remotePathToDelete = FileSystemUtils.GetJobClusterDirectoryPath(jobInfo.Specification, clusterConfig.InstanceIdentifierPath, clusterConfig.SubExecutionsPath).Replace("\\", "/");
        string username = jobInfo.Specification.ClusterUser?.Username ?? string.Empty;
        string expandedPath = FirecRestUtils.ExpandRemotePath(remotePathToDelete, username, jobInfo.Specification.Cluster.CustomConfiguration);

        var endpoint = $"{firecRestUrl}/filesystem/{systemName}/ops/rm?path={Uri.EscapeDataString(expandedPath.Replace("\\", "/"))}";

        _logger.LogDebug($"[FirecRest RM] DELETE {endpoint}");

        using var request = new HttpRequestMessage(HttpMethod.Delete, endpoint);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _httpClient.SendAsync(request);
        var responseContent = await response.Content.ReadAsStringAsync();
        _logger.LogDebug($"[FirecRest RM Response] Status: {response.StatusCode}, Content: {responseContent}");
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning($"Failed to delete session directory {expandedPath} on cluster {systemName}. Status: {response.StatusCode}, Response: {responseContent}");
        }
    }

    protected override async Task CopyAllAsync(string hostTimeZone, string source, string target, bool overwrite, DateTime? lastModificationLimit, string[] excludedFiles, ClusterAuthenticationCredentials credentials, Cluster cluster, string sshCaToken, string lexisToken)
    {
        throw new NotSupportedException("CopyAllAsync not supported for FirecREST filesystem manager.");
    }

    protected override async Task<ICollection<FileInformation>> ListChangedFilesForTaskAsync(string hostTimeZone, string taskClusterDirectoryPath, DateTime? jobSubmitTime, ClusterAuthenticationCredentials clusterAuthenticationCredentials, Cluster cluster, string sshCaToken, string lexisToken)
    {
        var (firecRestUrl, token) = await GetFirecrestUrlAndTokenAsync(cluster, lexisToken);
        string systemName = cluster.Name;

        string username = clusterAuthenticationCredentials?.Username ?? string.Empty;
        string expandedPath = FirecRestUtils.ExpandRemotePath(taskClusterDirectoryPath, username, cluster.CustomConfiguration);

        return await ListChangedFilesInDirectoryAsync(firecRestUrl, token, systemName, expandedPath, expandedPath, jobSubmitTime);
    }

    protected override IFileSynchronizer CreateFileSynchronizer(FullFileSpecification fileInfo, ClusterAuthenticationCredentials credentials, string sshCaToken, string lexisToken)
    {
        throw new NotSupportedException("Synchronizers not supported for FirecREST filesystem manager.");
    }

    public override async Task<bool> UploadFileToClusterByAbsolutePathAsync(Stream fileStream, string absoluteFilePath, ClusterAuthenticationCredentials credentials, Cluster cluster, string sshCaToken, string lexisToken)
    {
        try
        {
            var (firecRestUrl, token) = await GetFirecrestUrlAndTokenAsync(cluster, lexisToken);
            string systemName = cluster.Name;

            string username = credentials?.Username ?? string.Empty;
            string expandedPath = FirecRestUtils.ExpandRemotePath(absoluteFilePath, username, cluster.CustomConfiguration);

            string remoteDirectory = Path.GetDirectoryName(expandedPath)?.Replace("\\", "/");
            string fileName = Path.GetFileName(expandedPath);

            var endpoint = $"{firecRestUrl}/filesystem/{systemName}/ops/upload?path={Uri.EscapeDataString(remoteDirectory)}";
            _logger.LogDebug($"[FirecRest Upload] POST {endpoint} for file {fileName}");

            using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            using var content = new MultipartFormDataContent();
            
            using var ms = new MemoryStream();
            await fileStream.CopyToAsync(ms);
            byte[] fileBytes = ms.ToArray();
            if (fileBytes.Length == 0)
            {
                fileBytes = System.Text.Encoding.UTF8.GetBytes("\n");
            }

            var fileContentContent = new ByteArrayContent(fileBytes);
            fileContentContent.Headers.ContentType = MediaTypeHeaderValue.Parse("application/octet-stream");
            content.Add(fileContentContent, "file", fileName);

            request.Content = content;

            var response = await _httpClient.SendAsync(request);
            var responseContent = await response.Content.ReadAsStringAsync();
            _logger.LogDebug($"[FirecRest Upload Response] Status: {response.StatusCode}, Content: {responseContent}");
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError($"Failed to upload file {expandedPath} to cluster {systemName}. Status: {response.StatusCode}, Response: {responseContent}");
                throw new FirecrestApiException($"Failed to upload file. Status: {response.StatusCode}, Response: {responseContent}", response.StatusCode, responseContent);
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"An error occurred during file upload to {absoluteFilePath}: {ex.Message}");
            throw;
        }
    }

    public override async Task<bool> ModifyAbsolutePathFileAttributesAsync(string absoluteFilePath, ClusterAuthenticationCredentials credentials, Cluster cluster, string sshCaToken, string lexisToken, bool? ownerCanExecute = null, bool? groupCanExecute = null)
    {
        try
        {
            var (firecRestUrl, token) = await GetFirecrestUrlAndTokenAsync(cluster, lexisToken);
            string systemName = cluster.Name;

            string username = credentials?.Username ?? string.Empty;
            string expandedPath = FirecRestUtils.ExpandRemotePath(absoluteFilePath, username, cluster.CustomConfiguration);

            var endpoint = $"{firecRestUrl}/filesystem/{systemName}/ops/chmod";
            _logger.LogDebug($"[FirecRest Chmod] PUT {endpoint} for file {expandedPath}");

            using var request = new HttpRequestMessage(HttpMethod.Put, endpoint);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            string mode = "644";
            if ((ownerCanExecute.HasValue && ownerCanExecute.Value) || (groupCanExecute.HasValue && groupCanExecute.Value))
            {
                mode = "755";
            }

            var chmodPayload = new
            {
                sourcePath = expandedPath.Replace("\\", "/"),
                mode = mode
            };
            var jsonContent = JsonSerializer.Serialize(chmodPayload);
            request.Content = new StringContent(jsonContent, System.Text.Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request);
            var responseContent = await response.Content.ReadAsStringAsync();
            _logger.LogDebug($"[FirecRest Chmod Response] Status: {response.StatusCode}, Content: {responseContent}");
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError($"Failed to change permissions for {expandedPath} to {mode} on cluster {systemName}. Status: {response.StatusCode}, Response: {responseContent}");
                throw new FirecrestApiException($"Failed to change permissions. Status: {response.StatusCode}, Response: {responseContent}", response.StatusCode, responseContent);
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"An error occurred during file attributes modification on {absoluteFilePath}: {ex.Message}");
            return false;
        }
    }

    #endregion
}
