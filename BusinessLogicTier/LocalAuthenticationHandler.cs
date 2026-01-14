using System;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using HEAppE.BusinessLogicTier;
using HEAppE.BusinessLogicTier.Factory;
using HEAppE.DataAccessTier.Factory.UnitOfWork;
using HEAppE.DomainObjects.UserAndLimitationManagement.Authentication;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SshCaAPI;

public class LocalAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private const string ApiKeyHeaderName = "X-API-Key";
    
    private readonly ISshCertificateAuthorityService _sshCaService;
    private readonly IHttpContextKeys _httpContextKeys;

    public LocalAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        ISystemClock clock,
        ISshCertificateAuthorityService sshCaService,
        IHttpContextKeys httpContextKeys)
        : base(options, logger, encoder, clock)
    {
        _sshCaService = sshCaService;
        _httpContextKeys = httpContextKeys;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(ApiKeyHeaderName, out var extractedApiKey))
        {
            return AuthenticateResult.Fail("Missing API Key");
        }

        try
        {
            using (var unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
            {

                var match = Regex.Match(extractedApiKey, @"^([^:]+):(.+)$");
                if (!match.Success) return null;

                string username = match.Groups[1].Value;
                string rawApiKey = match.Groups[2].Value;
                var user = unitOfWork.AdaptorUserRepository.GetByName(username);
                if (user == null)
                {
                    return AuthenticateResult.Fail("Invalid Service API Key");
                }
                string salt = user.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss");
                string saltedPassword = rawApiKey + salt;
                string passwordHash = ComputeSha512Hash(saltedPassword);
                if (!string.Equals(passwordHash, user.Password, StringComparison.OrdinalIgnoreCase))
                {
                    return AuthenticateResult.Fail("Invalid Service API Key");
                }
                

                // Create claims based on the authenticated service user
                var claims = new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Username ?? "service-account"),
                    new Claim(ClaimTypes.Name, "Service API User"),
                    new Claim(ClaimTypes.Role, "ServiceRole"),
                    new Claim("auth_method", "ApiKey")
                };

                var identity = new ClaimsIdentity(claims, Scheme.Name);
                var principal = new ClaimsPrincipal(identity);
                var ticket = new AuthenticationTicket(principal, Scheme.Name);
                
                //set adaptor user id in context
                _httpContextKeys.Context.AdaptorUserId = user.Id; 

                return AuthenticateResult.Success(ticket);
            }
            
        }
        catch (System.Exception ex)
        {
            Logger.LogError(ex, "Error during API Key authentication");
            return AuthenticateResult.Fail("Authentication process failed");
        }
    }

    private static string ComputeSha512Hash(string input)
    {
        using var sha512 = System.Security.Cryptography.SHA512.Create();
        byte[] inputBytes = System.Text.Encoding.UTF8.GetBytes(input);
        byte[] hashBytes = sha512.ComputeHash(inputBytes);
        return Convert.ToHexString(hashBytes).ToUpper();
    }
}