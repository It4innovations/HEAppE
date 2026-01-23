using System.Globalization;
using System.Reflection;
using AspNetCoreRateLimit;
using FluentValidation;
using HEAppE.Authentication;
using HEAppE.BusinessLogicTier;
using HEAppE.BusinessLogicTier.Factory;
using HEAppE.DataAccessTier;
using HEAppE.DataAccessTier.Vault.Settings;
using HEAppE.DataStagingAPI;
using HEAppE.DataStagingAPI.API.AbstractTypes;
using HEAppE.DataStagingAPI.Configuration;
using HEAppE.ExternalAuthentication.Configuration;
using HEAppE.ExtModels;
using HEAppE.FileTransferFramework;
using HEAppE.HpcConnectionFramework.Configuration;
using log4net;
using MicroKnights.Log4NetHelper;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Localization;
using Microsoft.OpenApi.Models;
using SshCaAPI;
using SshCaAPI.Configuration;

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

builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 2L * 1024 * 1024 * 1024; // 2 GB
});

builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.Limits.MaxRequestBodySize = long.MaxValue;
});

builder.Configuration.Bind("SshCaSettings", new SshCaSettings());

//IPRateLimitation
builder.Services.Configure<IpRateLimitOptions>(builder.Configuration.GetSection("IpRateLimiting"));
builder.Services.Configure<IpRateLimitPolicies>(builder.Configuration.GetSection("IpRateLimitPolicies"));

builder.Configuration.Bind("HPCConnectionFrameworkSettings", new HPCConnectionFrameworkConfiguration());

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

// Lexis Token Service
builder.Services.AddHttpClient("LexisTokenExchangeClient");
builder.Services.AddSingleton<ILexisTokenService, LexisTokenService>();   


// Configurations
builder.Services.AddOptions<ApplicationAPIOptions>().BindConfiguration("ApplicationAPIConfiguration");

builder.Configuration.Bind("ExternalAuthenticationSettings", new ExternalAuthConfiguration());
builder.Configuration.Bind("VaultConnectorSettings", new VaultConnectorSettings());

var APIAdoptions = new ApplicationAPIOptions();
builder.Configuration.GetSection("ApplicationAPIConfiguration").Bind(APIAdoptions);

//add IUserOrgService
builder.Services.AddScoped<IUserOrgService, UserOrgService>();

builder.Services.AddHttpClient("userOrgApi", conf =>
{
    if (!string.IsNullOrEmpty(LexisAuthenticationConfiguration.BaseAddress))
        conf.BaseAddress = new Uri(LexisAuthenticationConfiguration.BaseAddress);
});

builder.Services.AddDistributedMemoryCache();
//if (JwtTokenIntrospectionConfiguration.LexisTokenFlowConfiguration.IsEnabled || LexisAuthenticationConfiguration.UseBearerAuth)
{
    builder.Services.AddHttpClient("LexisTokenExchangeClient");
    builder.Services.AddSingleton<ILexisTokenService, LexisTokenService>();
    builder.Services.AddAuthentication("Bearer");
    builder.Services.AddAuthorization();
}
if (true)
{
    builder.Services.AddSmartAuthentication(builder.Configuration);
}




//TODO Need to be delete after DI rework
MiddlewareContextSettings.ConnectionString = builder.Configuration.GetConnectionString("MiddlewareContext");

#pragma warning disable CS8604
var logRepository = LogManager.GetRepository(Assembly.GetEntryAssembly());
#pragma warning restore CS8604
GlobalContext.Properties["instanceName"] = APIAdoptions.DeploymentConfiguration.Name;
GlobalContext.Properties["instanceVersion"] = APIAdoptions.DeploymentConfiguration.Version;
GlobalContext.Properties["ip"] = APIAdoptions.DeploymentConfiguration.DeployedIPAddress;

AdoNetAppenderHelper.SetConnectionString(builder.Configuration.GetConnectionString("Logging"));

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddSwaggerGen(options =>
{
    options.SchemaFilter<PascalCasingPropertiesFilter>();
    options.SwaggerDoc(APIAdoptions.SwaggerConfiguration.Version, new OpenApiInfo
    {
        Version = APIAdoptions.SwaggerConfiguration.Version,
        Title = APIAdoptions.SwaggerConfiguration.Title,
        Description = APIAdoptions.SwaggerConfiguration.Description,
        TermsOfService = new Uri(APIAdoptions.SwaggerConfiguration.TermOfUsageUrl),
        License = new OpenApiLicense
        {
            Name = APIAdoptions.SwaggerConfiguration.License,
            Url = new Uri(APIAdoptions.SwaggerConfiguration.LicenseUrl)
        },
        Contact = new OpenApiContact
        {
            Name = APIAdoptions.SwaggerConfiguration.ContactName,
            Email = APIAdoptions.SwaggerConfiguration.ContactEmail,
            Url = new Uri(APIAdoptions.SwaggerConfiguration.ContactUrl)
        }
    });

    options.AddSecurityDefinition(APIAdoptions.AuthenticationParamHeaderName, new OpenApiSecurityScheme
    {
        Description = $"{APIAdoptions.AuthenticationParamHeaderName} must appear in header",
        Type = SecuritySchemeType.ApiKey,
        Name = APIAdoptions.AuthenticationParamHeaderName,
        In = ParameterLocation.Header,
        Scheme = $"{APIAdoptions.AuthenticationParamHeaderName}Scheme"
    });
    
    {
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
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    }
                },
                Array.Empty<string>()
            }
        });
    }
    var key = new OpenApiSecurityScheme
    {
        Reference = new OpenApiReference
        {
            Type = ReferenceType.SecurityScheme,
            Id = APIAdoptions.AuthenticationParamHeaderName
        },
        In = ParameterLocation.Header
    };
    var requirement = new OpenApiSecurityRequirement
    {
        { key, new List<string>() }
    };
    options.AddSecurityRequirement(requirement);
    
    options.AddSecurityDefinition("ServiceApiKey", new OpenApiSecurityScheme
    {
        Description = "Service API Key authentication. Enter the key below.",
        Name = "X-API-Key",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "ApiKeyScheme"
    });
    
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "ServiceApiKey" }
            },
            Array.Empty<string>()
        }
    });
});



//Localization and resources
builder.Services.AddLocalization();

builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    var supportedCultures = new List<CultureInfo>
    {
        new("en"),
        new("cs")
    };

    options.DefaultRequestCulture = new RequestCulture("en");
    options.SupportedCultures = supportedCultures;
});


//CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("HEAppEDefaultOrigins", builder =>
    {
        builder.WithOrigins(APIAdoptions.AllowedHosts)
            .AllowAnyHeader()
            .AllowAnyMethod();
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
// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment()) app.UseDeveloperExceptionPage();

//TODO Need to be delete after DI rework
ServiceActivator.Configure(app.Services);

app.UseCors("HEAppEDefaultOrigins");
app.UseMiddleware<ExceptionMiddleware>();
app.UseMiddleware<RequestSizeMiddleware>();
app.UseStatusCodePages();
app.UseIpRateLimiting();


app.RegisterApiRoutes();

app.UseSwagger(swagger =>
{
    swagger.PreSerializeFilters.Add((swaggerDoc, httpReq) =>
    {
        swaggerDoc.Servers = new List<OpenApiServer>
        {
            new()
            {
                Url = $"{APIAdoptions.SwaggerConfiguration.Host}/{APIAdoptions.SwaggerConfiguration.HostPostfix}"
            }
        };
    });
    
    var routePrefix = string.IsNullOrEmpty(APIAdoptions.SwaggerConfiguration.HostPostfix)
        ? string.Empty
        : APIAdoptions.SwaggerConfiguration.HostPostfix + "/";

    swagger.RouteTemplate = $"/{routePrefix}{APIAdoptions.SwaggerConfiguration.PrefixDocPath}/{{documentname}}/swagger.json";
});

app.UseSwaggerUI(swaggerUI =>
{
    var hostPrefix = string.IsNullOrEmpty(APIAdoptions.SwaggerConfiguration.HostPostfix)
        ? string.Empty
        : "/" + APIAdoptions.SwaggerConfiguration.HostPostfix;
        
    swaggerUI.SwaggerEndpoint(
        $"{hostPrefix}/{APIAdoptions.SwaggerConfiguration.PrefixDocPath}/{APIAdoptions.SwaggerConfiguration.Version}/swagger.json",
        APIAdoptions.SwaggerConfiguration.Title);

    swaggerUI.EnableTryItOutByDefault();
    
    if (!string.IsNullOrEmpty(APIAdoptions.SwaggerConfiguration.HostPostfix))
    {
        swaggerUI.RoutePrefix = $"{APIAdoptions.SwaggerConfiguration.HostPostfix}/{APIAdoptions.SwaggerConfiguration.PrefixDocPath}";
    }
    else
    {
        swaggerUI.RoutePrefix = APIAdoptions.SwaggerConfiguration.PrefixDocPath;
    }
});


app.UseMiddleware<LexisAuthMiddleware>();
app.UseMiddleware<LexisTokenExchangeMiddleware>();
{
    app.UseAuthentication();
    app.UseAuthorization();
}

app.Run();