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
        //var ticket = await _expirio.ExchangeTokenForKerberosAsync(request, _httpContextKeys.Context.LEXISToken, ct);
        var ticket = "BQQAAAAAAAEAAAABAAAABE1FVEEAAAALam9hb19jYXJpYXMAAAABAAAAAQAAAARNRVRBAAAAC2pvYW9fY2FyaWFzAAAAAgAAAAIAAAAETUVUQQAAAAZrcmJ0Z3QAAAAETUVUQQASAAAAIMH0xDIii76rFZ4q3B2mSGgbavuTCG0DJTUVJ97kk5KnaYHmtGmB5rVpgo91AAAAAABgKAAAAAAAAAAAAAAAAAFGYYIBQjCCAT6gAwIBBaEGGwRNRVRBohkwF6ADAgECoRAwDhsGa3JidGd0GwRNRVRBo4IBEjCCAQ6gAwIBEqEDAgEEooIBAASB/eSYNE1/16/11ElBjhKXnQx/LzseT6t6ZBMtSuPxzEHlN91tYFR3yDpFY0WiRHR1nObQ1HDRR2Ausw9xlXzb3kH7zDLOQGQUDvSB4wd7GtuX0W+tqVFslLD2/ndHbvWdtljVk0EjFiv4WEykTUfQ99XH52oknzvRemx3X/2JIPd5YWjYfOmJfBg/o9rDa6R9JgUNDZYntg2Zfp8RsA/BDSop5YqZrSxGoipqHbwWC2OPhXC9ozuRMG2K+rPbPRYHX6sk0VXHKuFKrxJXl7YyYhnCFrSD5kFafkFskOIawIaAlxQY50uClgh2fkFUxIJ6MKkVPXHu0O1RlCQkyPwAAAAA";
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
                        (bool isError1, string? line1) = await process.ReadLineAsync();
                        Console.WriteLine($"{msgInt}\nUser [{username}], service [{address}], stdout: {line1}\n{msgInt}\n");
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
