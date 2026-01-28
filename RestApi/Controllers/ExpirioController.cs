using System.Threading;
using System.Threading.Tasks;
using HEAppE.BusinessLogicTier;
using Microsoft.AspNetCore.Mvc;
using Services.Expirio;
using Services.Expirio.Models;


namespace HEAppE.RestApi.Controllers;


[ApiController]
[Route("api")]
public class ExpirioController : ControllerBase
{
    private readonly IExpirioService _expirio;
    private readonly IHttpContextKeys _httpContextKeys;

    public ExpirioController(IExpirioService expirio, IHttpContextKeys httpContextKeys)
    {
        _expirio = expirio;
        _httpContextKeys = httpContextKeys;
    } 

    [HttpPost("kerberos/exchange")]
    public async Task<IActionResult> GetKerberosTicket([FromBody] KerberosExchangeRequest request, CancellationToken ct)
    {
        var ticket = await _expirio.ExchangeTokenForKerberosAsync(request, _httpContextKeys.Context.LEXISToken, ct);
        return Ok(new { ticket });
    }

    [HttpPost("exchange")]
    public async Task<IActionResult> ExchangeToken([FromBody] ExchangeRequest request, CancellationToken ct)
    {
        var data = await _expirio.ExchangeTokenAsync(request, _httpContextKeys.Context.LEXISToken, ct);
        return Ok(new { data });
    }
}
