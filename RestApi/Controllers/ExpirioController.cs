using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Services.Expirio;
using Services.Expirio.Models;


namespace HEAppE.RestApi.Controllers;


[ApiController]
[Route("api/internal/expirio")]
public class ExpirioController : ControllerBase
{
    private readonly IExpirioService _expirio;

    public ExpirioController(IExpirioService expirio) => _expirio = expirio;

    [HttpPost("kerberos")]
    public async Task<IActionResult> GetKerberosTicket([FromBody] KerberosExchangeRequest request, CancellationToken ct)
    {
        var ticket = await _expirio.ExchangeTokenForKerberosAsync(request, ct);
        return Ok(new { ticket });
    }
}
