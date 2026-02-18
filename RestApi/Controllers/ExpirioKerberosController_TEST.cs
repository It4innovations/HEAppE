using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using HEAppE.BusinessLogicTier;
using HEAppE.BusinessLogicTier.AuthMiddleware;
using HEAppE.Services.Expirio;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Services.Expirio.Models;
using Tmds.Ssh;


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
        byte[] expirio_tkt = Convert.FromBase64String(ticket);
        string username = KrbLibSim.AddOrUpdateTicketCache(expirio_tkt);

        string test_address = "charon.nti.tul.cz";
        connect2ssh(username, test_address);

        return Ok(new { ticket });
    }


    private async void connect2ssh(string username, string address)
    {
        ILoggerFactory loggerFactory = LoggerFactory.Create(builder =>
                                                builder.SetMinimumLevel(LogLevel.Error)
                                                .AddConsole()
                                            );

        var sshConfigSettings = new SshConfigSettings();
        // clears SshConfigOption.SendEnv and others.
        sshConfigSettings.ConfigFilePaths.Clear();
        sshConfigSettings.Options.Add(SshConfigOption.User, new SshConfigOptionValue(username));
        sshConfigSettings.Options.Add(SshConfigOption.GSSAPIAuthentication, new SshConfigOptionValue("yes"));
        sshConfigSettings.Options.Add(SshConfigOption.GSSAPIDelegateCredentials, new SshConfigOptionValue("yes"));
        // StrictHostKeyChecking = "no" -> removes the need for known_hosts file key
        //TODO: this setting should be set as HEappE static option
        sshConfigSettings.Options.Add(SshConfigOption.StrictHostKeyChecking, new SshConfigOptionValue("no"));

        using (var client = new SshClient(address, sshConfigSettings, loggerFactory))
        {
            try
            {
                Console.WriteLine($"\n\n#############");
                Console.WriteLine($"# User [{username}] connecting to [{address}]...");

                var watch = Stopwatch.StartNew();
                // Connect to the SSH client
                await client.ConnectAsync();
                watch.Stop();

                string msg = $"--- User [{username}] connection to [{address}] successful! ({watch.ElapsedMilliseconds}ms) ---";
                string msgExt = new('@', msg.Length);
                string msgInt = new('-', msg.Length);

                //continue;
                string[] commands = ["whoami"];//, "hostnamectl", "pwd"];
                foreach (string command in commands)
                {
                    Console.WriteLine($"User [{username}] on service [{address}] executing remote command: {command}\n...\n");
                    using (var process = await client.ExecuteAsync(command))
                    {
                        (bool isError, string? line) = await process.ReadLineAsync();
                        Console.WriteLine($"{msgInt}\nUser [{username}], service [{address}], stdout: {line}\n{msgInt}\n");
                        //(string? stdout, string? stderr) = await process.ReadToEndAsStringAsync();
                        //Console.WriteLine($"stdout: {stdout}");
                    }
                }/**/
            }
            catch (Exception ex)
            {
                Console.WriteLine($"User [{username}], service [{address}] ### An error occurred: {ex.Message}");
                Console.WriteLine($"User [{username}], service [{address}] ### StackTrace: {ex.StackTrace}\n");
            }
        }
    }
}
