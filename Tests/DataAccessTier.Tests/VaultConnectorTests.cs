using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using HEAppE.DataAccessTier.Vault;
using HEAppE.DomainObjects.ClusterInformation;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace HEAppE.DataAccessTier.Tests;

[Trait("Category", "Unit")]
public class VaultConnectorTests
{
    private sealed class SequenceHttpMessageHandler : HttpMessageHandler
    {
        private readonly ConcurrentQueue<Func<HttpRequestMessage, HttpResponseMessage>> _responses = new();
        public int CallCount { get; private set; }

        public void Enqueue(Func<HttpRequestMessage, HttpResponseMessage> responder)
        {
            _responses.Enqueue(responder);
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            CallCount++;
            if (!_responses.TryDequeue(out var responder))
            {
                throw new InvalidOperationException($"Unexpected HTTP request #{CallCount} to {request.RequestUri}");
            }

            return Task.FromResult(responder(request));
        }
    }

    private static HttpResponseMessage CreateVaultOkResponse(long id, string privateKey)
    {
        var encodedKey = Convert.ToBase64String(Encoding.UTF8.GetBytes(privateKey));
        var json =
            $$"""
            {
              "data": {
                "data": {
                  "id": {{id}},
                  "password": "",
                  "privateKey": "{{encodedKey}}",
                  "privateKeyPassword": "",
                  "privateKeyCertificate": ""
                }
              }
            }
            """;

        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
    }

    [Fact]
    public async Task GetClusterAuthenticationCredentials_DoesNotCacheEmpty_WhenVaultTemporarilyFails()
    {
        var handler = new SequenceHttpMessageHandler();
        handler.Enqueue(_ => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));
        handler.Enqueue(_ => CreateVaultOkResponse(23, "-----BEGIN PRIVATE KEY-----\nTEST\n-----END PRIVATE KEY-----"));

        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://vaultagent:8100") };
        var cache = new ConcurrentDictionary<long, (Lazy<Task<ClusterProjectCredentialVaultPart>> Task, DateTime ExpiresAtUtc)>();
        var connector = new VaultConnector(NullLogger.Instance, httpClient, cache);

        var firstResult = await connector.GetClusterAuthenticationCredentials(23);
        firstResult.Should().Be(ClusterProjectCredentialVaultPart.Empty);

        // Next call must NOT hit a poisoned cache entry and must query Vault again
        var secondResult = await connector.GetClusterAuthenticationCredentials(23);
        secondResult.Should().NotBe(ClusterProjectCredentialVaultPart.Empty);
        secondResult.Id.Should().Be(23);
        Encoding.UTF8.GetString(Convert.FromBase64String(secondResult.PrivateKey)).Should().Contain("TEST");

        // Third call should hit the valid cache entry without making another HTTP call
        var thirdResult = await connector.GetClusterAuthenticationCredentials(23);
        thirdResult.Should().Be(secondResult);
        handler.CallCount.Should().Be(2);
    }

    [Fact]
    public async Task GetClusterAuthenticationCredentials_DoesNotCacheEmpty_WhenVaultReturnsInvalidJson()
    {
        var handler = new SequenceHttpMessageHandler();
        handler.Enqueue(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"errors\":[]}", Encoding.UTF8, "application/json")
        });
        handler.Enqueue(_ => CreateVaultOkResponse(42, "valid-key"));

        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://vaultagent:8100") };
        var cache = new ConcurrentDictionary<long, (Lazy<Task<ClusterProjectCredentialVaultPart>> Task, DateTime ExpiresAtUtc)>();
        var connector = new VaultConnector(NullLogger.Instance, httpClient, cache);

        var firstResult = await connector.GetClusterAuthenticationCredentials(42);
        firstResult.Should().Be(ClusterProjectCredentialVaultPart.Empty);

        var secondResult = await connector.GetClusterAuthenticationCredentials(42);
        secondResult.Id.Should().Be(42);
        handler.CallCount.Should().Be(2);
    }

    [Fact]
    public async Task GetClusterAuthenticationCredentials_EvictsCacheEntry_WhenRequestThrows()
    {
        var handler = new SequenceHttpMessageHandler();
        handler.Enqueue(_ => throw new TaskCanceledException("Simulated timeout"));
        handler.Enqueue(_ => CreateVaultOkResponse(7, "recovered-key"));

        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://vaultagent:8100") };
        var cache = new ConcurrentDictionary<long, (Lazy<Task<ClusterProjectCredentialVaultPart>> Task, DateTime ExpiresAtUtc)>();
        var connector = new VaultConnector(NullLogger.Instance, httpClient, cache);

        await Assert.ThrowsAsync<TaskCanceledException>(() => connector.GetClusterAuthenticationCredentials(7));

        var recoveredResult = await connector.GetClusterAuthenticationCredentials(7);
        recoveredResult.Id.Should().Be(7);
        handler.CallCount.Should().Be(2);
    }
}
