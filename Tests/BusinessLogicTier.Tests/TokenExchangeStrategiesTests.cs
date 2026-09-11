using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using HEAppE.BusinessLogicTier.AuthMiddleware;
using HEAppE.ExternalAuthentication.Configuration;
using HEAppE.Services.Expirio;
using HEAppE.Services.Expirio.Models;
using HEAppE.Services.TokenExchange;
using HEAppE.Services.TokenExchange.Strategies;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using Xunit;

namespace BusinessLogicTier.Tests;

[Trait("Category", "Unit")]
public class TokenExchangeStrategiesTests
{
    private readonly Mock<ILogger<Rfc8693ExchangeStrategy>> _rfcLoggerMock = new();
    private readonly Mock<ILogger<ExpirioExchangeStrategy>> _expirioLoggerMock = new();
    private readonly Mock<ILogger<ClientCredentialsExchangeStrategy>> _clientCredsLoggerMock = new();
    private readonly Mock<ILogger<KeycloakBrokerExchangeStrategy>> _brokerLoggerMock = new();

    private IHttpClientFactory CreateMockHttpClientFactory(HttpResponseMessage response)
    {
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);

        var httpClient = new HttpClient(handlerMock.Object);
        var factoryMock = new Mock<IHttpClientFactory>();
        factoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);
        return factoryMock.Object;
    }

    private IHttpClientFactory CreateMockHttpClientFactory(Func<HttpRequestMessage, HttpResponseMessage> responseHandler)
    {
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync((HttpRequestMessage req, CancellationToken ct) => responseHandler(req));

        var httpClient = new HttpClient(handlerMock.Object);
        var factoryMock = new Mock<IHttpClientFactory>();
        factoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);
        return factoryMock.Object;
    }

    [Fact]
    public async Task ExpirioStrategy_CallsExpirioService_AndReturnsSuccess()
    {
        // Arrange
        var target = new TokenExchangeTargetConfiguration
        {
            Name = "expirio-target",
            Strategy = "Expirio",
            ExpirioProviderName = "my-provider",
            ExpirioClientName = "my-client"
        };

        var expirioServiceMock = new Mock<IExpirioService>();
        expirioServiceMock.Setup(e => e.ExchangeTokenAsync(
                It.Is<ExchangeRequest>(r => r.ProviderName == "my-provider" && r.ClientName == "my-client"),
                "my-subject-token",
                It.IsAny<ILogger>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("expirio-ticket-abc");

        var strategy = new ExpirioExchangeStrategy(expirioServiceMock.Object, _expirioLoggerMock.Object);

        // Act
        var result = await strategy.ExchangeAsync(target, "my-subject-token");

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.AccessToken.Should().Be("expirio-ticket-abc");
        result.TargetName.Should().Be("expirio-target");
    }

    [Fact]
    public async Task ExpirioStrategy_WhenServiceReturnsEmpty_ReturnsFailed()
    {
        // Arrange
        var target = new TokenExchangeTargetConfiguration
        {
            Name = "expirio-target",
            Strategy = "Expirio"
        };

        var expirioServiceMock = new Mock<IExpirioService>();
        expirioServiceMock.Setup(e => e.ExchangeTokenAsync(
                It.IsAny<ExchangeRequest>(),
                It.IsAny<string>(),
                It.IsAny<ILogger>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(string.Empty);

        var strategy = new ExpirioExchangeStrategy(expirioServiceMock.Object, _expirioLoggerMock.Object);

        // Act
        var result = await strategy.ExchangeAsync(target, "subject-token");

        // Assert
        result.Success.Should().BeFalse();
        result.Error.Should().Contain("empty");
    }

    [Fact]
    public async Task Rfc8693Strategy_WithExplicitEndpoint_ParsesSuccessResponse()
    {
        // Arrange
        var target = new TokenExchangeTargetConfiguration
        {
            Name = "sshca",
            Strategy = "Rfc8693",
            TokenEndpoint = "https://iam.example.org/token",
            ClientId = "client1",
            ClientSecret = "secret1",
            Audience = "ssh-ca-service",
            Scope = "openid"
        };

        var responsePayload = new
        {
            access_token = "exchanged-jwt-token",
            token_type = "Bearer",
            expires_in = 300,
            issued_token_type = "urn:ietf:params:oauth:token-type:access_token"
        };

        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonSerializer.Serialize(responsePayload))
        };

        var factory = CreateMockHttpClientFactory(response);
        var strategy = new Rfc8693ExchangeStrategy(factory, _rfcLoggerMock.Object);

        // Act
        var result = await strategy.ExchangeAsync(target, "original-subject-token");

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.AccessToken.Should().Be("exchanged-jwt-token");
        result.ExpiresIn.Should().Be(300);
        result.IssuedTokenType.Should().Be("urn:ietf:params:oauth:token-type:access_token");
    }

    [Fact]
    public async Task Rfc8693Strategy_WhenEndpointReturnsError_ReturnsFailed()
    {
        // Arrange
        var target = new TokenExchangeTargetConfiguration
        {
            Name = "target-error",
            Strategy = "Rfc8693",
            TokenEndpoint = "https://iam.example.org/token"
        };

        var response = new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent("{\"error\":\"invalid_grant\"}")
        };

        var factory = CreateMockHttpClientFactory(response);
        var strategy = new Rfc8693ExchangeStrategy(factory, _rfcLoggerMock.Object);

        // Act
        var result = await strategy.ExchangeAsync(target, "subject-token");

        // Assert
        result.Success.Should().BeFalse();
        result.Error.Should().Contain("BadRequest");
    }

    [Fact]
    public async Task Rfc8693Strategy_WithDiscovery_FetchesWellKnownAndDiscoversEndpoint()
    {
        // Arrange
        var target = new TokenExchangeTargetConfiguration
        {
            Name = "disco-target",
            Strategy = "Rfc8693",
            Authority = "https://iam.example.org/auth",
            ClientId = "c",
            ClientSecret = "s"
        };

        var factory = CreateMockHttpClientFactory(req =>
        {
            if (req.RequestUri!.ToString().Contains(".well-known/openid-configuration"))
            {
                var disco = new { token_endpoint = "https://iam.example.org/auth/protocol/token" };
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(JsonSerializer.Serialize(disco))
                };
            }

            var tokenResp = new { access_token = "disco-exchanged-token", expires_in = 600 };
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(tokenResp))
            };
        });

        var strategy = new Rfc8693ExchangeStrategy(factory, _rfcLoggerMock.Object);

        // Act
        var result = await strategy.ExchangeAsync(target, "subject-token");

        // Assert
        result.Success.Should().BeTrue();
        result.AccessToken.Should().Be("disco-exchanged-token");
        result.ExpiresIn.Should().Be(600);
    }

    [Fact]
    public async Task ClientCredentialsStrategy_GetsToken_WithoutSubjectToken()
    {
        // Arrange
        var target = new TokenExchangeTargetConfiguration
        {
            Name = "firecrest-cc",
            Strategy = "ClientCredentials",
            TokenEndpoint = "https://firecrest.example.org/token",
            ClientId = "fc-client",
            ClientSecret = "fc-secret",
            Scope = "firecrest"
        };

        var responsePayload = new
        {
            access_token = "cc-access-token-123",
            token_type = "Bearer",
            expires_in = 3600
        };

        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonSerializer.Serialize(responsePayload))
        };

        var factory = CreateMockHttpClientFactory(response);
        var strategy = new ClientCredentialsExchangeStrategy(factory, _clientCredsLoggerMock.Object);

        // Act
        var result = await strategy.ExchangeAsync(target, "");

        // Assert
        result.Success.Should().BeTrue();
        result.AccessToken.Should().Be("cc-access-token-123");
        result.ExpiresIn.Should().Be(3600);
    }

    [Fact]
    public async Task KeycloakBrokerStrategy_PerformsTwoStepExchange_Successfully()
    {
        // Arrange
        var target = new TokenExchangeTargetConfiguration
        {
            Name = "idp-broker",
            Strategy = "KeycloakBroker",
            Authority = "https://iam.example.org/realms/lexis",
            TokenEndpoint = "https://iam.example.org/token",
            BrokerAlias = "target-broker"
        };

        var factory = CreateMockHttpClientFactory(req =>
        {
            // Step 1: RFC 8693
            if (req.RequestUri!.ToString().Contains("/token") && !req.RequestUri.ToString().Contains("/broker/"))
            {
                var rfcResp = new { access_token = "intermediate-token" };
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(JsonSerializer.Serialize(rfcResp))
                };
            }

            // Step 2: Broker token
            if (req.RequestUri.ToString().Contains("/broker/target-broker/token"))
            {
                var brokerResp = new { access_token = "final-idp-token", expires_in = 1800 };
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(JsonSerializer.Serialize(brokerResp))
                };
            }

            return new HttpResponseMessage(HttpStatusCode.NotFound);
        });

        var rfcStrategy = new Rfc8693ExchangeStrategy(factory, _rfcLoggerMock.Object);
        var brokerStrategy = new KeycloakBrokerExchangeStrategy(rfcStrategy, factory, _brokerLoggerMock.Object);

        // Act
        var result = await brokerStrategy.ExchangeAsync(target, "raw-token");

        // Assert
        result.Success.Should().BeTrue();
        result.AccessToken.Should().Be("final-idp-token");
        result.ExpiresIn.Should().Be(1800);
    }

    [Fact]
    public void RequestContext_UserOrgToken_IntegratesWithExchangedTokens()
    {
        // Arrange
        var context = new RequestContext();

        // Act
        context.UserOrgToken = "userorg-custom-token";

        // Assert
        context.UserOrgToken.Should().Be("userorg-custom-token");
        context.GetExchangedToken("userorg").Should().Be("userorg-custom-token");
        context.ExchangedTokens["userorg"].Should().Be("userorg-custom-token");

        // Setting via ExchangedTokens dictionary reflects in property
        context.ExchangedTokens["userorg"] = "updated-userorg-token";
        context.UserOrgToken.Should().Be("updated-userorg-token");

        // Case-insensitivity check
        context.GetExchangedToken("USERORG").Should().Be("updated-userorg-token");
    }
}
