using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace HEAppE.RestApi.IntegrationTests.Infrastructure;

public class ApiClient
{
    private readonly HttpClient _client;
    private readonly JsonSerializerOptions _jsonOptions;

    public ApiClient(HttpClient client)
    {
        _client = client;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = null
        };
    }

    public void SetApiKey(string username, string key = "Passw0rd")
    {
        _client.DefaultRequestHeaders.Remove("X-API-Key");
        _client.DefaultRequestHeaders.Add("X-API-Key", $"{username}:{key}");
    }

    public void ClearAuth()
    {
        _client.DefaultRequestHeaders.Remove("X-API-Key");
        _client.DefaultRequestHeaders.Authorization = null;
    }

    public async Task<HttpResponseMessage> GetAsync(string url)
    {
        return await _client.GetAsync(url);
    }

    public async Task<T> GetJsonAsync<T>(string url)
    {
        var response = await _client.GetAsync(url);
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<T>(content, _jsonOptions);
    }

    public async Task<HttpResponseMessage> PostJsonAsync<T>(string url, T data)
    {
        var json = JsonSerializer.Serialize(data, _jsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        return await _client.PostAsync(url, content);
    }

    public async Task<TResult> PostJsonAsync<TRequest, TResult>(string url, TRequest data)
    {
        var response = await PostJsonAsync(url, data);
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<TResult>(content, _jsonOptions);
    }

    public async Task<HttpResponseMessage> PutJsonAsync<T>(string url, T data)
    {
        var json = JsonSerializer.Serialize(data, _jsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        return await _client.PutAsync(url, content);
    }

    public async Task<HttpResponseMessage> DeleteAsync(string url)
    {
        return await _client.DeleteAsync(url);
    }
}
