using System;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using HEAppE.BusinessLogicTier;
using HEAppE.BusinessLogicTier.AuthMiddleware;
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
        if (!Request.Headers.ContainsKey(ApiKeyHeaderName))
        {
            this.Logger.LogInformation($"[LocalAuth] No {ApiKeyHeaderName} header found. Path: {Request.Path}");
            
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "InternalUser"),
                new Claim(ClaimTypes.Name, "Internal User"),
                new Claim(ClaimTypes.Role, "InternalRole"),
                new Claim("auth_method", "Internal")
            };
            var identity = new ClaimsIdentity(claims, Scheme.Name);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, Scheme.Name);
            
            this.Logger.LogDebug("[LocalAuth] Proceeding with InternalUser identity.");
            return AuthenticateResult.Success(ticket);
        }
        
        if (!Request.Headers.TryGetValue(ApiKeyHeaderName, out var extractedApiKey))
        {
            this.Logger.LogWarning($"[LocalAuth] {ApiKeyHeaderName} present but empty. Path: {Request.Path}");
            return AuthenticateResult.Fail("Missing API Key");
        }

        try
        {
            using (var unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
            {
                this.Logger.LogInformation("[LocalAuth] Attempting Service API Key authentication.");
                
                var match = Regex.Match(extractedApiKey, @"^([^:]+):(.+)$");
                if (!match.Success) 
                {
                    this.Logger.LogWarning("[LocalAuth] API Key format is invalid (expected 'user:key').");
                    return AuthenticateResult.Fail("Invalid API Key format");
                }

                string username = match.Groups[1].Value;
                string rawApiKey = match.Groups[2].Value;

                this.Logger.LogDebug($"[LocalAuth] Looking up user: {username}");
                var user = unitOfWork.AdaptorUserRepository.GetByName(username);
                
                if (user == null)
                {
                    this.Logger.LogWarning($"[LocalAuth] User '{username}' not found in database.");
                    return AuthenticateResult.Fail("Invalid Service API Key");
                }

                string salt = user.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss");
                string saltedPassword = rawApiKey + salt;
                string passwordHash = ComputeSha512Hash(saltedPassword);

                if (!string.Equals(passwordHash, user.Password, StringComparison.OrdinalIgnoreCase))
                {
                    this.Logger.LogWarning($"[LocalAuth] API Key hash mismatch for user '{username}'.");
                    return AuthenticateResult.Fail("Invalid Service API Key");
                }

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
                
                _httpContextKeys.Context.AdaptorUserId = user.Id; 
                this.Logger.LogInformation("[LocalAuth] Success for user {userID}({username}). Path: {path}", user.Id, user.Username, Request.Path);

                return AuthenticateResult.Success(ticket);
            }
        }
        catch (System.Exception ex)
        {
            Logger.LogError(ex, "[LocalAuth] Exception during authentication process.");
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