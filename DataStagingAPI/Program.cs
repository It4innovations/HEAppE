using System.Globalization;
using System.Net;
using System.Reflection;
using AspNetCoreRateLimit;
using FluentValidation;
using HEAppE.Authentication;
using HEAppE.BusinessLogicTier;
using HEAppE.BusinessLogicTier.AuthMiddleware;
using HEAppE.BusinessLogicTier.Configuration;
using HEAppE.BusinessLogicTier.Factory;
using HEAppE.CertificateGenerator.Configuration;
using HEAppE.DataAccessTier;
using HEAppE.DataAccessTier.Configuration;
using HEAppE.DataAccessTier.Vault.Settings;
using HEAppE.DataStagingAPI;
using HEAppE.DataStagingAPI.API.AbstractTypes;
using HEAppE.DataStagingAPI.Configuration;
using HEAppE.ExternalAuthentication.Configuration;
using HEAppE.ExtModels;
using HEAppE.FileTransferFramework;
using HEAppE.HpcConnectionFramework.Configuration;
using HEAppE.OpenStackAPI.Configuration;
using HEAppE.RestApi.Logging;
using HEAppE.Services.AuthMiddleware;
using HEAppE.Services.Expirio;
using HEAppE.Services.UserOrg;
using HEAppE.ServiceTier.FileTransfer;
using log4net;
using MicroKnights.Log4NetHelper;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.Http;
using Microsoft.OpenApi.Models;
using Polly;
using Polly.Extensions.Http;
using Services.Expirio.Configuration;
using SshCaAPI;
using SshCaAPI.Configuration;
using HEAppE.BackgroundThread.Configuration;
using HEAppE.RestApi.Configuration;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddMemoryCache();
builder.Logging.ClearProviders();

if (Environment.GetEnvironmentVariable("ASPNETCORE_RUNTYPE_ENVIRONMENT") == "Docker")
{
    builder.Logging.AddLog4Net("Logging/log4netDocker.config");
    builder.Configuration.AddJsonFile("/opt/heappe/confs/appsettings.json", false, false);
    builder.Configuration.AddJsonFile("/opt/heappe/confs/appsettings-data.json", false, false);
}
else
{
    builder.Logging.AddLog4Net("Logging/log4net.config");
    if (!HEAppE.Utils.FileSystemUtils.AddConfigurationFiles(
        confsDirs: [
            Directory.GetCurrentDirectory(),
            "/opt/heappe/confs",
            "P:\\source\\localHEAppE\\confs"
        ],
        confFiles: [
            "appsettings.json",
            "appsettings-data.json",
        ],
        addJsonFile: confPath => builder.Configuration.AddJsonFile(confPath, false, false))
    )
        throw new Exception("Configuration files not found!");
}

builder.Configuration.Bind("BackGroundThreadSettings", new BackGroundThreadConfiguration());
builder.Configuration.Bind("DatabaseFullBackupSettings", new DatabaseFullBackupConfiguration());
builder.Configuration.Bind("DatabaseTransactionLogBackupSettings", new DatabaseTransactionLogBackupConfiguration());
builder.Configuration.Bind("DatabaseBackupSettings", new DatabaseFullBackupConfiguration());
builder.Configuration.Bind("BusinessLogicSettings", new BusinessLogicConfiguration());
builder.Configuration.Bind("RoleAssignments", new RoleAssignmentConfiguration());
builder.Configuration.Bind("CertificateGeneratorSettings", new CertificateGeneratorConfiguration());
builder.Configuration.Bind("MiddlewareContextSettings", new MiddlewareContextSettings());
MiddlewareContextSettings.ConnectionString = builder.Configuration.GetConnectionString("MiddlewareContext");
builder.Configuration.Bind("DatabaseMigrationSettings", new DatabaseMigrationSettings());
builder.Configuration.Bind("HPCConnectionFrameworkSettings", new HPCConnectionFrameworkConfiguration());
builder.Configuration.Bind("ApplicationAPISettings", new ApplicationAPIConfiguration());
builder.Configuration.Bind("ExternalAuthenticationSettings", new ExternalAuthConfiguration());
builder.Configuration.Bind("OpenStackSettings", new OpenStackSettings());
builder.Configuration.Bind("VaultConnectorSettings", new VaultConnectorSettings());
builder.Configuration.Bind("SshCaSettings", new SshCaSettings());
builder.Configuration.Bind("HealthCheckSettings", new HealthCheckSettings());
builder.Configuration.Bind("ExpirioSettings", new ExpirioSettings());
builder.Configuration.Bind("JwtTokenIntrospectionConfiguration", new JwtTokenIntrospectionConfiguration());


var globalRetryPolicy = HttpPolicyExtensions
    .HandleTransientHttpError()
    .OrResult(msg => msg.StatusCode == HttpStatusCode.TooManyRequests)
    .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
        onRetry: (outcome, timespan, retryCount, context) =>
        {
            LogManager.GetLogger("RetryPolicy").Warn($"Retry {retryCount} after {timespan.TotalSeconds}s: {outcome.Exception?.Message ?? outcome.Result.StatusCode.ToString()}");
        });

builder.Services.ConfigureAll<HttpClientFactoryOptions>(options =>
{
    options.HttpMessageHandlerBuilderActions.Add(builder =>
    {
        builder.AdditionalHandlers.Add(new PolicyHttpMessageHandler(globalRetryPolicy));
    });
});

builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 2L * 1024 * 1024 * 1024;
});

builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.Limits.MaxRequestBodySize = long.MaxValue;
    serverOptions.Limits.MinRequestBodyDataRate = null;
    serverOptions.Limits.MinResponseDataRate = null;
});

builder.Services.Configure<IpRateLimitOptions>(builder.Configuration.GetSection("IpRateLimiting"));
builder.Services.Configure<IpRateLimitPolicies>(builder.Configuration.GetSection("IpRateLimitPolicies"));

builder.Services.AddInMemoryRateLimiting();
builder.Services.AddSingleton<IIpPolicyStore, MemoryCacheIpPolicyStore>();
builder.Services.AddSingleton<IRateLimitCounterStore, MemoryCacheRateLimitCounterStore>();
builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();
builder.Services.AddSingleton<IProcessingStrategy, AsyncKeyLockProcessingStrategy>();

builder.Services.AddSingleton<ISshCertificateAuthorityService>(sp => new SshCertificateAuthorityService(
    SshCaSettings.BaseUri,
    SshCaSettings.CAName,
    SshCaSettings.ConnectionTimeoutInSeconds
));

builder.Services.AddScoped<IHttpContextKeys, HttpContextKeys>();
builder.Services.AddScoped<IRequestContext, RequestContext>();
builder.Services.AddSingleton<ILexisTokenService, LexisTokenService>();
builder.Services.AddOptions<ApplicationAPIOptions>().BindConfiguration("ApplicationAPIConfiguration");

var APIAdoptions = new ApplicationAPIOptions();
builder.Configuration.GetSection("ApplicationAPIConfiguration").Bind(APIAdoptions);

builder.Services.AddScoped<IExpirioService, ExpirioService>();

builder.Services.AddHttpClient("ExpirioClient", conf =>
{
    conf.BaseAddress = new Uri(ExpirioSettings.BaseUrl);
    conf.Timeout = TimeSpan.FromSeconds(ExpirioSettings.TimeoutSeconds);
    conf.DefaultRequestHeaders.Add("Accept", "application/json");
});

builder.Services.AddSingleton<IUserOrgService, UserOrgService>();
builder.Services.AddScoped<FileTransferService>();

builder.Services.AddHttpClient("userOrgApi", conf =>
{
    if (!string.IsNullOrEmpty(LexisAuthenticationConfiguration.BaseAddress))
        conf.BaseAddress = new Uri(LexisAuthenticationConfiguration.BaseAddress);
});

builder.Services.AddDistributedMemoryCache();
builder.Services.AddHttpClient("LexisTokenExchangeClient");
builder.Services.AddAuthentication("Bearer");
builder.Services.AddAuthorization();

if (true)
{
    builder.Services.AddSmartAuthentication(builder.Configuration);
}

#pragma warning disable CS8604
var logRepository = LogManager.GetRepository(Assembly.GetEntryAssembly());
#pragma warning restore CS8604
GlobalContext.Properties["instanceName"] = APIAdoptions.DeploymentConfiguration.Name;
GlobalContext.Properties["instanceVersion"] = APIAdoptions.DeploymentConfiguration.Version;
GlobalContext.Properties["ip"] = APIAdoptions.DeploymentConfiguration.DeployedIPAddress;

AdoNetAppenderHelper.SetConnectionString(builder.Configuration.GetConnectionString("Logging"));

builder.Services.AddSwaggerGen(options =>
{
    options.SchemaFilter<PascalCasingPropertiesFilter>();
    options.SwaggerDoc(APIAdoptions.SwaggerConfiguration.Version, new OpenApiInfo
    {
        Version = APIAdoptions.SwaggerConfiguration.Version,
        Title = APIAdoptions.SwaggerConfiguration.Title,
        Description = APIAdoptions.SwaggerConfiguration.Description
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });

    options.AddSecurityDefinition("ServiceApiKey", new OpenApiSecurityScheme
    {
        Description = "Service API Key authentication.",
        Name = "X-API-Key",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "ApiKeyScheme"
    });
});

builder.Services.AddLocalization();
builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    var supportedCultures = new List<CultureInfo> { new("en"), new("cs") };
    options.DefaultRequestCulture = new RequestCulture("en");
    options.SupportedCultures = supportedCultures;
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("HEAppEDefaultOrigins", b =>
    {
        b.WithOrigins(APIAdoptions.AllowedHosts).AllowAnyHeader().AllowAnyMethod();
    });
});

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = null;
    options.SerializerOptions.PropertyNameCaseInsensitive = true;
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddValidatorsFromAssemblyContaining<IAssemblyMarker>(ServiceLifetime.Singleton);

var app = builder.Build();
LogicFactory.ServiceProvider = app.Services;

if (app.Environment.IsDevelopment()) app.UseDeveloperExceptionPage();
ServiceActivator.Configure(app.Services);

var pathBase = APIAdoptions.SwaggerConfiguration.HostPostfix;
if (!string.IsNullOrEmpty(pathBase))
{
    if (!pathBase.StartsWith("/")) pathBase = "/" + pathBase;
    app.UsePathBase(pathBase);
}

app.UseCors("HEAppEDefaultOrigins");
app.UseMiddleware<RequestSizeMiddleware>();
app.UseStatusCodePages();
app.UseIpRateLimiting();

app.UseSwagger(swagger =>
{
    swagger.PreSerializeFilters.Add((swaggerDoc, httpReq) =>
    {
        swaggerDoc.Servers = new List<OpenApiServer> { new() { Url = $"{APIAdoptions.SwaggerConfiguration.Host}/{APIAdoptions.SwaggerConfiguration.HostPostfix}" } };
    });
    swagger.RouteTemplate = $"{APIAdoptions.SwaggerConfiguration.PrefixDocPath}/{{documentname}}/swagger.json";
});

app.UseSwaggerUI(swaggerUI =>
{
    var hostPrefix = string.IsNullOrEmpty(APIAdoptions.SwaggerConfiguration.HostPostfix) ? string.Empty : "/" + APIAdoptions.SwaggerConfiguration.HostPostfix;
    swaggerUI.SwaggerEndpoint($"{hostPrefix}/{APIAdoptions.SwaggerConfiguration.PrefixDocPath}/{APIAdoptions.SwaggerConfiguration.Version}/swagger.json", APIAdoptions.SwaggerConfiguration.Title);
    swaggerUI.RoutePrefix = APIAdoptions.SwaggerConfiguration.PrefixDocPath;
});

app.UseMiddleware<LogUserContextMiddleware>();
app.UseMiddleware<LexisAuthMiddleware>();
app.UseMiddleware<LexisTokenExchangeMiddleware>();
app.UseAuthentication();
app.UseMiddleware<ExceptionMiddleware>();
app.UseAuthorization();

app.RegisterApiRoutes();
app.Run();