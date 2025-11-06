using System.Globalization;
using System.Reflection;
using AspNetCoreRateLimit;
using FluentValidation;
using HEAppE.Authentication;
using HEAppE.BusinessLogicTier.Factory;
using HEAppE.DataAccessTier;
using HEAppE.DataStagingAPI;
using HEAppE.DataStagingAPI.API.AbstractTypes;
using HEAppE.DataStagingAPI.Configuration;
using HEAppE.ExternalAuthentication.Configuration;
using HEAppE.ExtModels;
using HEAppE.FileTransferFramework;
using log4net;
using MicroKnights.Log4NetHelper;
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

builder.Configuration.Bind("SshCaSettings", new SshCaSettings());

//IPRateLimitation
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

// Configurations
builder.Services.AddOptions<ApplicationAPIOptions>().BindConfiguration("ApplicationAPIConfiguration");

builder.Configuration.Bind("ExternalAuthenticationSettings", new ExternalAuthConfiguration());

var APIAdoptions = new ApplicationAPIOptions();
builder.Configuration.GetSection("ApplicationAPIConfiguration").Bind(APIAdoptions);

builder.Services.AddHttpClient("userOrgApi", conf =>
{
    if (!string.IsNullOrEmpty(LexisAuthenticationConfiguration.BaseAddress))
        conf.BaseAddress = new Uri(LexisAuthenticationConfiguration.BaseAddress);
});

builder.Services.AddDistributedMemoryCache();
builder.Services.AddJwtIntrospectionIfEnabled(builder.Configuration);

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
    if (JwtTokenIntrospectionConfiguration.IsEnabled)
    {
        options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token in the text input below.",
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
//get ISshCertificateAuthorityService instance from DI
var sshCertificateAuthorityService = app.Services.GetRequiredService<ISshCertificateAuthorityService>();
app.RegisterApiRoutes(sshCertificateAuthorityService);

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
    swagger.RouteTemplate = $"/{APIAdoptions.SwaggerConfiguration.PrefixDocPath}/{{documentname}}/swagger.json";
});

app.UseSwaggerUI(swaggerUI =>
{
    var hostPrefix = string.IsNullOrEmpty(APIAdoptions.SwaggerConfiguration.HostPostfix)
        ? string.Empty
        : "/" + APIAdoptions.SwaggerConfiguration.HostPostfix;
    swaggerUI.SwaggerEndpoint(
        $"{hostPrefix}/{APIAdoptions.SwaggerConfiguration.PrefixDocPath}/{APIAdoptions.SwaggerConfiguration.Version}/swagger.json",
        APIAdoptions.SwaggerConfiguration.Title);
    swaggerUI.RoutePrefix = APIAdoptions.SwaggerConfiguration.PrefixDocPath;
    swaggerUI.EnableTryItOutByDefault();
});

app.Run();