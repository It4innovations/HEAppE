using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Net;
using Microsoft.Extensions.Http;
using Polly;
using Polly.Extensions.Http;
using AspNetCoreRateLimit;
using HEAppE.Authentication;
using HEAppE.BackgroundThread;
using HEAppE.BackgroundThread.Configuration;
using HEAppE.BusinessLogicTier;
using HEAppE.BusinessLogicTier.Configuration;
using HEAppE.BusinessLogicTier.Factory;
using HEAppE.BusinessLogicTier.Logic.UserAndLimitationManagement;
using HEAppE.CertificateGenerator.Configuration;
using HEAppE.DataAccessTier;
using HEAppE.DataAccessTier.Configuration;
using HEAppE.DataAccessTier.Vault.Settings;
using HEAppE.DomainObjects.UserAndLimitationManagement;
using HEAppE.DomainObjects.UserAndLimitationManagement.Authentication;
using HEAppE.ExternalAuthentication.Configuration;
using HEAppE.ExtModels.UserAndLimitationManagement.Models;
using HEAppE.FileTransferFramework;
using HEAppE.HpcConnectionFramework.Configuration;
using HEAppE.OpenStackAPI.Configuration;
using HEAppE.RestApi.Authentication;
using HEAppE.RestApi.Configuration;
using HEAppE.RestApi.Logging;
using HEAppE.ServiceTier.UserAndLimitationManagement;
using IdentityModel.AspNetCore.OAuth2Introspection;
using IdentityModel.Client;
using log4net;
using MicroKnights.Log4NetHelper;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.AspNetCore.Rewrite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Microsoft.OpenApi.Models;
using SshCaAPI;
using SshCaAPI.Configuration;
using JwtTokenIntrospectionConfiguration = HEAppE.ExternalAuthentication.Configuration.JwtTokenIntrospectionConfiguration;
using Services.Expirio;
using Services.Expirio.Configuration;
using HEAppE.BusinessLogicTier.AuthMiddleware;
using HEAppE.Services.AuthMiddleware;
using HEAppE.Services.Expirio;
using HEAppE.Services.UserOrg;

namespace HEAppE.RestApi;

public class Startup
{
    private readonly string _allowSpecificOrigins = "HEAppEDefaultOrigins";

    public Startup(IConfiguration configuration)
    {
        Configuration = configuration;
    }

    public IConfiguration Configuration { get; }

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddBackgroundServices();
        services.AddMemoryCache();

        services.Configure<IpRateLimitOptions>(Configuration.GetSection("IpRateLimiting"));
        services.Configure<IpRateLimitPolicies>(Configuration.GetSection("IpRateLimitPolicies"));

        Configuration.Bind("BackGroundThreadSettings", new BackGroundThreadConfiguration());
        Configuration.Bind("DatabaseFullBackupSettings", new DatabaseFullBackupConfiguration());
        Configuration.Bind("DatabaseTransactionLogBackupSettings", new DatabaseTransactionLogBackupConfiguration());
        Configuration.Bind("DatabaseBackupSettings", new DatabaseFullBackupConfiguration());
        Configuration.Bind("BusinessLogicSettings", new BusinessLogicConfiguration());
        Configuration.Bind("RoleAssignments", new RoleAssignmentConfiguration());
        Configuration.Bind("CertificateGeneratorSettings", new CertificateGeneratorConfiguration());
        Configuration.Bind("MiddlewareContextSettings", new MiddlewareContextSettings());
        MiddlewareContextSettings.ConnectionString = Configuration.GetConnectionString("MiddlewareContext");
        Configuration.Bind("DatabaseMigrationSettings", new DatabaseMigrationSettings());
        Configuration.Bind("HPCConnectionFrameworkSettings", new HPCConnectionFrameworkConfiguration());
        Configuration.Bind("ApplicationAPISettings", new ApplicationAPIConfiguration());
        Configuration.Bind("ExternalAuthenticationSettings", new ExternalAuthConfiguration());
        Configuration.Bind("OpenStackSettings", new OpenStackSettings());
        Configuration.Bind("VaultConnectorSettings", new VaultConnectorSettings());
        Configuration.Bind("SshCaSettings", new SshCaSettings());
        Configuration.Bind("HealthCheckSettings", new HealthCheckSettings());
        Configuration.Bind("ExpirioSettings", new ExpirioSettings());
        Configuration.Bind("JwtTokenIntrospectionConfiguration", new JwtTokenIntrospectionConfiguration());

        services.AddSingleton<IIpPolicyStore, MemoryCacheIpPolicyStore>();
        services.AddSingleton<IRateLimitCounterStore, MemoryCacheRateLimitCounterStore>();
        services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
        services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();
        services.AddSingleton<IProcessingStrategy, AsyncKeyLockProcessingStrategy>();
        services.AddSingleton<ISshCertificateAuthorityService>(sp => new SshCertificateAuthorityService(
            SshCaSettings.BaseUri,
            SshCaSettings.CAName,
            SshCaSettings.ConnectionTimeoutInSeconds
        ));

        services.AddSingleton<SqlServerHealthCheck>();
        services.AddSingleton<VaultHealthCheck>();

        var retryPolicy = HttpPolicyExtensions
            .HandleTransientHttpError()
            .OrResult(msg => msg.StatusCode == HttpStatusCode.TooManyRequests)
            .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                onRetry: (outcome, timespan, retryCount, context) =>
                {
                    LogManager.GetLogger("RetryPolicy").Warn($"Retry {retryCount} after {timespan.TotalSeconds}s: {outcome.Exception?.Message ?? outcome.Result.StatusCode.ToString()}");
                });

        services.ConfigureAll<HttpClientFactoryOptions>(options =>
        {
            options.HttpMessageHandlerBuilderActions.Add(builder =>
            {
                builder.AdditionalHandlers.Add(new PolicyHttpMessageHandler(retryPolicy));
            });
        });

        services.AddControllers(options =>
        {
            options.Filters.Add<LogRequestModelFilter>();
            options.Filters.Add(new AuthorizeFilter());
        }).AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.PropertyNamingPolicy = null;
            options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
        });

        services.AddSingleton<IUserOrgService, UserOrgService>();

        services.AddHttpClient("userOrgApi", conf =>
        {
            if (!string.IsNullOrEmpty(LexisAuthenticationConfiguration.BaseAddress))
            {
                conf.BaseAddress = new Uri(LexisAuthenticationConfiguration.BaseAddress);
                conf.Timeout = TimeSpan.FromSeconds(60);
            }
        });

        services.AddScoped<IExpirioService, ExpirioService>();

        services.AddHttpClient("ExpirioClient", conf =>
        {
            conf.BaseAddress = new Uri(ExpirioSettings.BaseUrl);
            conf.Timeout = TimeSpan.FromSeconds(ExpirioSettings.TimeoutSeconds);
            conf.DefaultRequestHeaders.Add("Accept", "application/json");
        })
        .AddTransientHttpErrorPolicy(p => p.CircuitBreakerAsync(5, TimeSpan.FromSeconds(ExpirioSettings.TimeoutSeconds)));

        services.AddScoped<IUserAndLimitationManagementLogic, UserAndLimitationManagementLogic>();
        services.AddScoped<IRequestContext, RequestContext>();
        services.AddScoped<IHttpContextKeys, HttpContextKeys>();

        services.AddSmartAuthentication(Configuration);

        services.AddCors(options =>
        {
            options.AddPolicy(_allowSpecificOrigins, builder =>
            {
                builder.WithOrigins(ApplicationAPIConfiguration.AllowedHosts)
                    .AllowAnyHeader()
                    .AllowAnyMethod();
            });
        });

        services.AddHttpClient("LexisTokenExchangeClient");
        services.AddSingleton<ILexisTokenService, LexisTokenService>();

        services.AddSwaggerGen(gen =>
        {
            gen.AddSecurityDefinition("ServiceApiKey", new OpenApiSecurityScheme
            {
                Name = "X-API-Key",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.ApiKey,
                Scheme = "ApiKeyScheme"
            });

            gen.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "ServiceApiKey" }
                    },
                    Array.Empty<string>()
                }
            });

            gen.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.ApiKey,
                Scheme = "Bearer",
                BearerFormat = "JWT"
            });

            gen.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
                    },
                    Array.Empty<string>()
                }
            });
            
            gen.ParameterFilter<PascalCaseParameterFilter>();
            gen.SwaggerDoc(SwaggerConfiguration.Version, new OpenApiInfo { Title = SwaggerConfiguration.Title, Version = SwaggerConfiguration.Version });
            gen.SwaggerDoc("DetailedJobReporting", new OpenApiInfo { Title = "Detailed Job Reporting API", Version = SwaggerConfiguration.Version });
            gen.SwaggerDoc("py4heappe", new OpenApiInfo { Title = "py4heappe API", Version = SwaggerConfiguration.Version });

            gen.DocInclusionPredicate((documentName, apiDescription) =>
            {
                if (documentName == "DetailedJobReporting") return apiDescription.GroupName == "DetailedJobReporting";
                if (documentName == SwaggerConfiguration.Version) return string.IsNullOrEmpty(apiDescription.GroupName);
                if (documentName == "py4heappe") return true;
                return false;
            });

            var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            gen.IncludeXmlComments(xmlPath);
        });

        services.AddRazorPages();
        services.AddLocalization();
        services.Configure<RequestLocalizationOptions>(options =>
        {
            var supportedCultures = new List<CultureInfo> { new("en"), new("cs") };
            options.DefaultRequestCulture = new RequestCulture("en");
            options.SupportedCultures = supportedCultures;
        });

        services.AddHealthChecks()
            .AddCheck<SqlServerHealthCheck>("sql")
            .AddCheck<VaultHealthCheck>("vault");
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ILoggerFactory loggerFactory)
    {
        LogicFactory.ServiceProvider = app.ApplicationServices;
        var logRepository = LogManager.GetRepository(Assembly.GetEntryAssembly());
        GlobalContext.Properties["instanceName"] = DeploymentInformationsConfiguration.Name;
        GlobalContext.Properties["instanceVersion"] = DeploymentInformationsConfiguration.Version;
        GlobalContext.Properties["ip"] = DeploymentInformationsConfiguration.DeployedIPAddress;

        if (Environment.GetEnvironmentVariable("ASPNETCORE_RUNTYPE_ENVIRONMENT") == "Docker")
            loggerFactory.AddLog4Net("Logging/log4netDocker.config");
        else
            loggerFactory.AddLog4Net("Logging/log4net.config");

        AdoNetAppenderHelper.SetConnectionString(Configuration.GetConnectionString("Logging"));

        ServiceActivator.Configure(app.ApplicationServices);
        if (env.IsDevelopment()) app.UseDeveloperExceptionPage();

        app.UseIpRateLimiting();
        app.UseStatusCodePages();
        app.UseStaticFiles();
        
        app.UseSwagger(swagger =>
        {
            swagger.PreSerializeFilters.Add((swaggerDoc, httpReq) =>
            {
                swaggerDoc.Servers = new List<OpenApiServer> { new() { Url = $"{SwaggerConfiguration.Host}/{SwaggerConfiguration.HostPostfix}" } };
            });
            swagger.RouteTemplate = $"/{SwaggerConfiguration.PrefixDocPath}/{{documentname}}/swagger.json";
            swagger.OpenApiVersion = OpenApiSpecVersion.OpenApi3_0;
        });

        app.UseSwaggerUI(swaggerUI =>
        {
            var hostPrefix = string.IsNullOrEmpty(SwaggerConfiguration.HostPostfix) ? string.Empty : "/" + SwaggerConfiguration.HostPostfix;
            swaggerUI.SwaggerEndpoint($"{hostPrefix}/{SwaggerConfiguration.PrefixDocPath}/{SwaggerConfiguration.Version}/swagger.json", SwaggerConfiguration.Title);
            swaggerUI.RoutePrefix = SwaggerConfiguration.PrefixDocPath;
            swaggerUI.EnableTryItOutByDefault();
        });

        app.UseRequestLocalization();
        app.UseRouting();
        app.UseMiddleware<LogUserContextMiddleware>();
        app.UseMiddleware<LexisAuthMiddleware>();
        app.UseMiddleware<LexisTokenExchangeMiddleware>();
        app.UseAuthentication();
        app.UseMiddleware<ExceptionMiddleware>();
        app.UseAuthorization();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}");
            endpoints.MapRazorPages();
        });

        app.UseCors(_allowSpecificOrigins);

        var option = new RewriteOptions();
        option.AddRedirect("^$", $"{SwaggerConfiguration.HostPostfix}/swagger/index.html");
        app.UseRewriter(option);
    }
}