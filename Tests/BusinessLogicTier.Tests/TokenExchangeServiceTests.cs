using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using HEAppE.BusinessLogicTier.AuthMiddleware;
using HEAppE.ExternalAuthentication.Configuration;
using HEAppE.Services.TokenExchange;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace BusinessLogicTier.Tests;

[Trait("Category", "Unit")]
public class TokenExchangeServiceTests
{
    private readonly Mock<ILogger<TokenExchangeService>> _loggerMock = new();

    private IConfiguration CreateConfiguration(Dictionary<string, string?> values)
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();
    }

    [Fact]
    public async Task ExchangeAsync_WithValidTargetAndStrategy_ReturnsSuccess()
    {
        // Arrange
        var target = new TokenExchangeTargetConfiguration
        {
            Name = "sshca",
            IsEnabled = true,
            Strategy = "Rfc8693",
            Audience = "ssh-ca-service"
        };

        var strategyMock = new Mock<ITokenExchangeStrategy>();
        strategyMock.Setup(s => s.StrategyName).Returns("Rfc8693");
        strategyMock.Setup(s => s.ExchangeAsync(target, "subject-token", It.IsAny<CancellationToken>()))
            .ReturnsAsync(TokenExchangeResult.Succeeded("sshca", "exchanged-sshca-token"));

        var config = CreateConfiguration(new());
        var service = new TokenExchangeService(
            new[] { strategyMock.Object },
            new[] { target },
            config,
            _loggerMock.Object);

        // Act
        var result = await service.ExchangeAsync("sshca", "subject-token");

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.AccessToken.Should().Be("exchanged-sshca-token");
        result.TargetName.Should().Be("sshca");
    }

    [Fact]
    public async Task ExchangeAsync_WhenTargetDisabled_ReturnsFailed()
    {
        // Arrange
        var target = new TokenExchangeTargetConfiguration
        {
            Name = "disabled-target",
            IsEnabled = false,
            Strategy = "Rfc8693"
        };

        var config = CreateConfiguration(new());
        var service = new TokenExchangeService(
            Array.Empty<ITokenExchangeStrategy>(),
            new[] { target },
            config,
            _loggerMock.Object);

        // Act
        var result = await service.ExchangeAsync("disabled-target", "subject-token");

        // Assert
        result.Success.Should().BeFalse();
        result.Error.Should().Contain("disabled");
    }

    [Fact]
    public async Task ExchangeAsync_WhenTargetNotConfigured_ReturnsFailed()
    {
        // Arrange
        var config = CreateConfiguration(new());
        var service = new TokenExchangeService(
            Array.Empty<ITokenExchangeStrategy>(),
            Array.Empty<TokenExchangeTargetConfiguration>(),
            config,
            _loggerMock.Object);

        // Act
        var result = await service.ExchangeAsync("non-existent", "subject-token");

        // Assert
        result.Success.Should().BeFalse();
        result.Error.Should().Contain("not configured");
    }

    [Fact]
    public async Task ExecuteAutoExchangesAsync_ResolvesChainedDependenciesInCorrectOrder()
    {
        // Arrange
        // Graph: incoming -> idp -> sshca
        //                  \-> userorg
        var executionOrder = new List<string>();

        var idpTarget = new TokenExchangeTargetConfiguration
        {
            Name = "idp",
            IsEnabled = true,
            Strategy = "KeycloakBroker",
            SubjectTokenSource = "incoming",
            AutoExchange = true,
            AutoExchangePhase = "PreAuth"
        };

        var sshcaTarget = new TokenExchangeTargetConfiguration
        {
            Name = "sshca",
            IsEnabled = true,
            Strategy = "Rfc8693",
            SubjectTokenSource = "idp",
            AutoExchange = true,
            AutoExchangePhase = "PreAuth"
        };

        var userorgTarget = new TokenExchangeTargetConfiguration
        {
            Name = "userorg",
            IsEnabled = true,
            Strategy = "Rfc8693",
            SubjectTokenSource = "idp",
            AutoExchange = true,
            AutoExchangePhase = "PreAuth"
        };

        var brokerStrategy = new Mock<ITokenExchangeStrategy>();
        brokerStrategy.Setup(s => s.StrategyName).Returns("KeycloakBroker");
        brokerStrategy.Setup(s => s.ExchangeAsync(idpTarget, "raw-bearer-token", It.IsAny<CancellationToken>()))
            .Callback(() => executionOrder.Add("idp"))
            .ReturnsAsync(TokenExchangeResult.Succeeded("idp", "exchanged-idp-token"));

        var rfcStrategy = new Mock<ITokenExchangeStrategy>();
        rfcStrategy.Setup(s => s.StrategyName).Returns("Rfc8693");
        rfcStrategy.Setup(s => s.ExchangeAsync(sshcaTarget, "exchanged-idp-token", It.IsAny<CancellationToken>()))
            .Callback(() => executionOrder.Add("sshca"))
            .ReturnsAsync(TokenExchangeResult.Succeeded("sshca", "exchanged-sshca-token"));
        rfcStrategy.Setup(s => s.ExchangeAsync(userorgTarget, "exchanged-idp-token", It.IsAny<CancellationToken>()))
            .Callback(() => executionOrder.Add("userorg"))
            .ReturnsAsync(TokenExchangeResult.Succeeded("userorg", "exchanged-userorg-token"));

        // Shuffle targets deliberately to verify topological sort orders them correctly
        var targets = new[] { userorgTarget, sshcaTarget, idpTarget };
        var config = CreateConfiguration(new());

        var service = new TokenExchangeService(
            new[] { brokerStrategy.Object, rfcStrategy.Object },
            targets,
            config,
            _loggerMock.Object);

        var exchangedTokens = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        // Act
        var results = await service.ExecuteAutoExchangesAsync("PreAuth", "raw-bearer-token", exchangedTokens);

        // Assert
        results.Should().HaveCount(3);
        results["idp"].Success.Should().BeTrue();
        results["sshca"].Success.Should().BeTrue();
        results["userorg"].Success.Should().BeTrue();

        // "idp" MUST execute before both "sshca" and "userorg"
        executionOrder.IndexOf("idp").Should().BeLessThan(executionOrder.IndexOf("sshca"));
        executionOrder.IndexOf("idp").Should().BeLessThan(executionOrder.IndexOf("userorg"));

        // Tokens should be stored in the dictionary
        exchangedTokens["idp"].Should().Be("exchanged-idp-token");
        exchangedTokens["sshca"].Should().Be("exchanged-sshca-token");
        exchangedTokens["userorg"].Should().Be("exchanged-userorg-token");
    }

    [Fact]
    public async Task ExecuteAutoExchangesAsync_CircularDependency_ThrowsInvalidOperationException()
    {
        // Arrange
        // A depends on B, B depends on A
        var targetA = new TokenExchangeTargetConfiguration
        {
            Name = "targetA",
            IsEnabled = true,
            Strategy = "Rfc8693",
            SubjectTokenSource = "targetB",
            AutoExchange = true,
            AutoExchangePhase = "PostAuth"
        };

        var targetB = new TokenExchangeTargetConfiguration
        {
            Name = "targetB",
            IsEnabled = true,
            Strategy = "Rfc8693",
            SubjectTokenSource = "targetA",
            AutoExchange = true,
            AutoExchangePhase = "PostAuth"
        };

        var strategyMock = new Mock<ITokenExchangeStrategy>();
        strategyMock.Setup(s => s.StrategyName).Returns("Rfc8693");

        var config = CreateConfiguration(new());
        var service = new TokenExchangeService(
            new[] { strategyMock.Object },
            new[] { targetA, targetB },
            config,
            _loggerMock.Object);

        var exchangedTokens = new Dictionary<string, string>();

        // Act & Assert
        var act = async () => await service.ExecuteAutoExchangesAsync("PostAuth", "incoming", exchangedTokens);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Circular dependency*");
    }

    [Fact]
    public async Task ExecuteAutoExchangesAsync_RespectsAutoExchangeCondition()
    {
        // Arrange
        var targetEnabled = new TokenExchangeTargetConfiguration
        {
            Name = "targetEnabled",
            IsEnabled = true,
            Strategy = "Rfc8693",
            SubjectTokenSource = "incoming",
            AutoExchange = true,
            AutoExchangePhase = "PostAuth",
            AutoExchangeCondition = "FeatureA:IsEnabled"
        };

        var targetDisabled = new TokenExchangeTargetConfiguration
        {
            Name = "targetDisabled",
            IsEnabled = true,
            Strategy = "Rfc8693",
            SubjectTokenSource = "incoming",
            AutoExchange = true,
            AutoExchangePhase = "PostAuth",
            AutoExchangeCondition = "FeatureB:IsEnabled"
        };

        var strategyMock = new Mock<ITokenExchangeStrategy>();
        strategyMock.Setup(s => s.StrategyName).Returns("Rfc8693");
        strategyMock.Setup(s => s.ExchangeAsync(targetEnabled, "incoming", It.IsAny<CancellationToken>()))
            .ReturnsAsync(TokenExchangeResult.Succeeded("targetEnabled", "tokenA"));

        var config = CreateConfiguration(new()
        {
            ["FeatureA:IsEnabled"] = "true",
            ["FeatureB:IsEnabled"] = "false"
        });

        var service = new TokenExchangeService(
            new[] { strategyMock.Object },
            new[] { targetEnabled, targetDisabled },
            config,
            _loggerMock.Object);

        var exchangedTokens = new Dictionary<string, string>();

        // Act
        var results = await service.ExecuteAutoExchangesAsync("PostAuth", "incoming", exchangedTokens);

        // Assert
        results.Should().ContainKey("targetEnabled");
        results.Should().NotContainKey("targetDisabled");
        exchangedTokens.Should().ContainKey("targetEnabled");
        exchangedTokens.Should().NotContainKey("targetDisabled");
    }

    [Fact]
    public void HasAutoExchangeTargets_ReturnsTrueOnlyWhenTargetActiveForPhase()
    {
        // Arrange
        var target = new TokenExchangeTargetConfiguration
        {
            Name = "postAuthTarget",
            IsEnabled = true,
            AutoExchange = true,
            AutoExchangePhase = "PostAuth",
            AutoExchangeCondition = "Feature:Enabled"
        };

        var configActive = CreateConfiguration(new() { ["Feature:Enabled"] = "true" });
        var configInactive = CreateConfiguration(new() { ["Feature:Enabled"] = "false" });

        var serviceActive = new TokenExchangeService(
            Array.Empty<ITokenExchangeStrategy>(),
            new[] { target },
            configActive,
            _loggerMock.Object);

        var serviceInactive = new TokenExchangeService(
            Array.Empty<ITokenExchangeStrategy>(),
            new[] { target },
            configInactive,
            _loggerMock.Object);

        // Act & Assert
        serviceActive.HasAutoExchangeTargets("PostAuth").Should().BeTrue();
        serviceActive.HasAutoExchangeTargets("PreAuth").Should().BeFalse();
        serviceInactive.HasAutoExchangeTargets("PostAuth").Should().BeFalse();
    }

    [Fact]
    public void RequestContext_ExchangedTokens_BackwardCompatibility()
    {
        // Arrange
        var context = new RequestContext();

        // Act & Assert - SshCaToken getter/setter integrates with ExchangedTokens
        context.SshCaToken = "custom-ssh-token";
        context.GetExchangedToken("sshca").Should().Be("custom-ssh-token");
        context.ExchangedTokens["sshca"].Should().Be("custom-ssh-token");

        // Act & Assert - IdpToken getter/setter integrates with ExchangedTokens
        context.IdpToken = "custom-idp-token";
        context.GetExchangedToken("idp").Should().Be("custom-idp-token");
        context.ExchangedTokens["idp"].Should().Be("custom-idp-token");

        // Act & Assert - Direct ExchangedTokens insertion reflects in properties
        context.ExchangedTokens["sshca"] = "new-ssh-token";
        context.SshCaToken.Should().Be("new-ssh-token");

        // Case-insensitivity check
        context.GetExchangedToken("SSHCA").Should().Be("new-ssh-token");
        context.GetExchangedToken("IDP").Should().Be("custom-idp-token");
    }
}
