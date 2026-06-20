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
using HEAppE.Utils;

namespace HEAppE.FileTransferFramework.FirecRest;

public class FirecRestFileSystemManager : AbstractFileSystemManager
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IExpirioService _expirio;
    private HttpClient _httpClient => _httpClientFactory.CreateClient("FirecREST");

    public FirecRestFileSystemManager(ILogger logger, FileTransferMethod configuration,
        FileSystemFactory synchronizerFactory, IHttpClientFactory httpClientFactory, IExpirioService expirio)
        : base(logger, configuration, synchronizerFactory)
    {
        _httpClientFactory = httpClientFactory;
        _expirio = expirio;
    }

    private async Task<(string Url, string Token)> GetFirecrestUrlAndTokenAsync(Cluster cluster, string userToken)
    {
        string protocol = cluster.ConnectionProtocol == ClusterConnectionProtocol.Http ? "http" : "https";
        string url = $"{protocol}://{cluster.MasterNodeName}";
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
            if (options.TryGetValue("f7t_client_id", out var value))
                clientId = value;
            if (options.TryGetValue("f7t_client_secret", out value))
                clientSecret = value;
            if (options.TryGetValue("f7t_url", out value))
                url = value;
            if (options.TryGetValue("f7t_token_url", out value))
                idpUrl = value;
        }

        if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
        {
            throw new FirecRestException("FirecRest credentials must be obtained from Expirio. Direct access via Proxy Connection is not allowed.")
            {
                CommandError = "Missing Expirio credentials"
            };
        }

        var token = await GetAuthTokenAsync(clientId, clientSecret, idpUrl);
        return (url.TrimEnd('/'), token);
    }

    private async Task<string> GetAuthTokenAsync(string clientId, string clientSecret, string firecRestIdpUrl)
    {
        try
        {
            var tokenRequestContent = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("grant_type", "client_credentials"),
                new KeyValuePair<string, string>("client_id", clientId),
                new KeyValuePair<string, string>("client_secret", clientSecret)
            });

            using var request = new HttpRequestMessage(HttpMethod.Post, firecRestIdpUrl);
            request.Content = tokenRequestContent;

            var tokenResponse = await _httpClient.SendAsync(request);
            if (!tokenResponse.IsSuccessStatusCode)
            {
                var errorContent = await tokenResponse.Content.ReadAsStringAsync();
                throw new FirecRestException($"Failed to obtain OAuth2 token for FirecRest API. Status: {tokenResponse.StatusCode}. Response: {errorContent}")
                {
                    CommandError = "Token request failed"
                };
            }

            var responseContent = await tokenResponse.Content.ReadAsStringAsync();
            var tokenData = JsonSerializer.Deserialize<JsonElement>(responseContent);

            if (tokenData.TryGetProperty("access_token", out var accessTokenElement))
            {
                return accessTokenElement.GetString();
            }

            throw new FirecRestException("Invalid OAuth2 response: access_token not found")
            {
                CommandError = "Missing access token"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to retrieve FirecRest authentication token: {ex.Message}");
            throw;
        }
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

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError($"Failed to list files in directory {currentDirectory} on cluster {systemName}. Status: {response.StatusCode}, Response: {responseContent}");
            throw new FirecrestApiException($"Failed to list files. Status: {response.StatusCode}, Response: {responseContent}", response.StatusCode, responseContent);
        }

        using var doc = JsonDocument.Parse(responseContent);
        if (doc.RootElement.ValueKind == JsonValueKind.Array)
        {
            foreach (var element in doc.RootElement.EnumerateArray())
            {
                string name = element.GetProperty("name").GetString();
                if (name == "." || name == "..") continue;

                string type = element.GetProperty("type").GetString();
                string fullPath = $"{currentDirectory.TrimEnd('/')}/{name}";

                if (type == "d")
                {
                    results.AddRange(await ListChangedFilesInDirectoryAsync(firecRestUrl, token, systemName, rootDirectory, fullPath, lastModificationLimit));
                }
                else
                {
                    DateTime lastModifiedDate = DateTime.MinValue;
                    if (element.TryGetProperty("lastModified", out var lmProp) && lmProp.GetString() is { } lmStr)
                    {
                        DateTime.TryParse(lmStr, out lastModifiedDate);
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

        var localBasePath = Path.Combine(basePath, _scripts.SubExecutionsPath.TrimStart('/'));
        var partPath = localBasePath.Replace(basePath, string.Empty);
        var file = Path.Combine(basePath, _scripts.InstanceIdentifierPath, partPath.TrimStart('/'), jobInfo.Specification.ClusterUser.Username, relativeFilePath.TrimStart('/'));

        return await DownloadFileFromClusterByAbsolutePathAsync(jobInfo.Specification, file, sshCaToken, lexisToken);
    }

    public override async Task<byte[]> DownloadFileFromClusterByAbsolutePathAsync(JobSpecification jobSpecification, string absoluteFilePath, string sshCaToken, string lexisToken)
    {
        var (firecRestUrl, token) = await GetFirecrestUrlAndTokenAsync(jobSpecification.Cluster, lexisToken);
        string systemName = jobSpecification.Cluster.Name;

        var endpoint = $"{firecRestUrl}/filesystem/{systemName}/ops/view?path={Uri.EscapeDataString(absoluteFilePath.Replace("\\", "/"))}";
        _logger.LogDebug($"[FirecRest View] GET {endpoint}");

        using var request = new HttpRequestMessage(HttpMethod.Get, endpoint);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _httpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            var responseContent = await response.Content.ReadAsStringAsync();
            _logger.LogError($"Failed to download file {absoluteFilePath} from cluster {systemName}. Status: {response.StatusCode}, Response: {responseContent}");
            throw new FirecrestApiException($"Failed to download file. Status: {response.StatusCode}, Response: {responseContent}", response.StatusCode, responseContent);
        }

        return await response.Content.ReadAsByteArrayAsync();
    }

    public override async Task DeleteSessionFromClusterAsync(SubmittedJobInfo jobInfo, string sshCaToken, string lexisToken)
    {
        var (firecRestUrl, token) = await GetFirecrestUrlAndTokenAsync(jobInfo.Specification.Cluster, lexisToken);
        string systemName = jobInfo.Specification.Cluster.Name;

        string remotePathToDelete = FileSystemUtils.GetJobClusterDirectoryPath(jobInfo.Specification, _scripts.InstanceIdentifierPath, _scripts.SubExecutionsPath).Replace("\\", "/");
        var endpoint = $"{firecRestUrl}/filesystem/{systemName}/ops/rm?path={Uri.EscapeDataString(remotePathToDelete)}";

        _logger.LogDebug($"[FirecRest RM] DELETE {endpoint}");

        using var request = new HttpRequestMessage(HttpMethod.Delete, endpoint);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _httpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            var responseContent = await response.Content.ReadAsStringAsync();
            _logger.LogWarning($"Failed to delete session directory {remotePathToDelete} on cluster {systemName}. Status: {response.StatusCode}, Response: {responseContent}");
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

        return await ListChangedFilesInDirectoryAsync(firecRestUrl, token, systemName, taskClusterDirectoryPath, taskClusterDirectoryPath, jobSubmitTime);
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

            string remoteDirectory = Path.GetDirectoryName(absoluteFilePath)?.Replace("\\", "/");
            string fileName = Path.GetFileName(absoluteFilePath);

            var endpoint = $"{firecRestUrl}/filesystem/{systemName}/ops/upload?path={Uri.EscapeDataString(remoteDirectory)}";
            _logger.LogDebug($"[FirecRest Upload] POST {endpoint} for file {fileName}");

            using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            using var content = new MultipartFormDataContent();
            
            using var ms = new MemoryStream();
            await fileStream.CopyToAsync(ms);
            byte[] fileBytes = ms.ToArray();

            var fileContentContent = new ByteArrayContent(fileBytes);
            fileContentContent.Headers.ContentType = MediaTypeHeaderValue.Parse("application/octet-stream");
            content.Add(fileContentContent, "file", fileName);

            request.Content = content;

            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                _logger.LogError($"Failed to upload file {absoluteFilePath} to cluster {systemName}. Status: {response.StatusCode}, Response: {responseContent}");
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

            var endpoint = $"{firecRestUrl}/filesystem/{systemName}/ops/chmod";
            _logger.LogDebug($"[FirecRest Chmod] PUT {endpoint} for file {absoluteFilePath}");

            using var request = new HttpRequestMessage(HttpMethod.Put, endpoint);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            string mode = "644";
            if ((ownerCanExecute.HasValue && ownerCanExecute.Value) || (groupCanExecute.HasValue && groupCanExecute.Value))
            {
                mode = "755";
            }

            var chmodPayload = new
            {
                sourcePath = absoluteFilePath.Replace("\\", "/"),
                mode = mode
            };
            var jsonContent = JsonSerializer.Serialize(chmodPayload);
            request.Content = new StringContent(jsonContent, System.Text.Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                _logger.LogError($"Failed to change permissions for {absoluteFilePath} to {mode} on cluster {systemName}. Status: {response.StatusCode}, Response: {responseContent}");
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
