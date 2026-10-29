using System;
using System.Threading;
using System.Threading.Tasks;
using HEAppE.ExternalAuthentication.Configuration;
using HEAppE.Services.Expirio;
using HEAppE.Services.Expirio.Models;
using Microsoft.Extensions.Logging;

namespace HEAppE.Services.TokenExchange.Strategies;

public class ExpirioExchangeStrategy : ITokenExchangeStrategy
{
    private readonly IExpirioService _expirioService;
    private readonly ILogger<ExpirioExchangeStrategy> _logger;

    public string StrategyName => "Expirio";

    public ExpirioExchangeStrategy(IExpirioService expirioService, ILogger<ExpirioExchangeStrategy> logger)
    {
        _expirioService = expirioService;
        _logger = logger;
    }

    public async Task<TokenExchangeResult> ExchangeAsync(
        TokenExchangeTargetConfiguration target,
        string subjectToken,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var request = new ExchangeRequest
            {
                ProviderName = target.ExpirioProviderName,
                ClientName = target.ExpirioClientName
            };

            var ticket = await _expirioService.ExchangeTokenAsync(request, subjectToken, _logger, cancellationToken);
            
            if (string.IsNullOrEmpty(ticket))
            {
                return TokenExchangeResult.Failed(target.Name, "Expirio returned an empty ticket.");
            }

            return TokenExchangeResult.Succeeded(target.Name, ticket);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception during Expirio token exchange for target {Target}.", target.Name);
            return TokenExchangeResult.Failed(target.Name, ex.Message);
        }
    }
}
