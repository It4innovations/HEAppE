using AspNetCoreRateLimit;
using FluentValidation;
using HEAppE.DataAccessTier;
using HEAppE.DataStagingAPI;
using HEAppE.DataStagingAPI.API.AbstractTypes;
using HEAppE.DataStagingAPI.Configuration;
using HEAppE.ExtModels;
using HEAppE.FileTransferFramework;
using log4net;
using MicroKnights.Log4NetHelper;
using Microsoft.AspNetCore.Localization;
using Microsoft.OpenApi.Models;
using System.Globalization;
using System.Reflection;
using System.Text.Json.Serialization;
using HEAppE.BusinessLogicTier.Factory;
using HEAppE.ExternalAuthentication.Configuration;

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
}

//IPRateLimitation
builder.Services.Configure<IpRateLimitOptions>(builder.Configuration.GetSection("IpRateLimiting"));
builder.Services.Configure<IpRateLimitPolicies>(builder.Configuration.GetSection("IpRateLimitPolicies"));

builder.Services.AddInMemoryRateLimiting();

builder.Services.AddSingleton<IIpPolicyStore, MemoryCacheIpPolicyStore>();
builder.Services.AddSingleton<IRateLimitCounterStore, MemoryCacheRateLimitCounterStore>();
builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();
builder.Services.AddSingleton<IProcessingStrategy, AsyncKeyLockProcessingStrategy>();

// Configurations
builder.Services.AddOptions<ApplicationAPIOptions>().BindConfiguration("ApplicationAPIConfiguration");

builder.Configuration.Bind("ExternalAuthenticationSettings", new ExternalAuthConfiguration());

var APIAdoptions = new ApplicationAPIOptions();
builder.Configuration.GetSection("ApplicationAPIConfiguration").Bind(APIAdoptions);

builder.Services.AddHttpClient("userOrgApi", conf =>
{
    conf.BaseAddress = new Uri(LexisAuthenticationConfiguration.BaseAddress);
});

//TODO Need to be delete after DI rework
MiddlewareContextSettings.ConnectionString = builder.Configuration.GetConnectionString("MiddlewareContext");

var logRepository = LogManager.GetRepository(Assembly.GetEntryAssembly());
GlobalContext.Properties["instanceName"] = APIAdoptions.DeploymentConfiguration.Name;
GlobalContext.Properties["instanceVersion"] = APIAdoptions.DeploymentConfiguration.Version;
GlobalContext.Properties["ip"] = APIAdoptions.DeploymentConfiguration.DeployedIPAddress;

AdoNetAppenderHelper.SetConnectionString(builder.Configuration.GetConnectionString("Logging"));

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc(APIAdoptions.SwaggerConfiguration.Version, new OpenApiInfo
    {
        Version = APIAdoptions.SwaggerConfiguration.Version,
        Title = APIAdoptions.SwaggerConfiguration.Title,
        Description = APIAdoptions.SwaggerConfiguration.Description,
        TermsOfService = new Uri(APIAdoptions.SwaggerConfiguration.TermOfUsageUrl),
        License = new OpenApiLicense()
        {
            Name = APIAdoptions.SwaggerConfiguration.License,
            Url = new Uri(APIAdoptions.SwaggerConfiguration.LicenseUrl),
        },
        Contact = new OpenApiContact()
        {
            Name = APIAdoptions.SwaggerConfiguration.ContactName,
            Email = APIAdoptions.SwaggerConfiguration.ContactEmail,
            Url = new Uri(APIAdoptions.SwaggerConfiguration.ContactUrl),
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
    var key = new OpenApiSecurityScheme()
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
                    new CultureInfo("en"),
                    new CultureInfo("cs")
                };

    options.DefaultRequestCulture = new RequestCulture("en");
    options.SupportedCultures = supportedCultures;
});


//CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy(name: "HEAppEDefaultOrigins", builder =>
    {
        builder.WithOrigins(APIAdoptions.AllowedHosts)
                .AllowAnyHeader()
                .AllowAnyMethod();
    });
});

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
    options.SerializerOptions.PropertyNamingPolicy = null;
    options.SerializerOptions.PropertyNameCaseInsensitive = true;
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddValidatorsFromAssemblyContaining<IAssemblyMarker>(ServiceLifetime.Singleton);

var app = builder.Build();
LogicFactory.ServiceProvider = app.Services;
// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

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
        swaggerDoc.Servers = new List<OpenApiServer> {
                        new OpenApiServer{
                            Url =  $"{APIAdoptions.SwaggerConfiguration.Host}/{APIAdoptions.SwaggerConfiguration.HostPostfix}"
                        }
                    };
    });
    swagger.RouteTemplate = $"/{APIAdoptions.SwaggerConfiguration.PrefixDocPath}/{{documentname}}/swagger.json";
});

app.UseSwaggerUI(swaggerUI =>
{
    string hostPrefix = string.IsNullOrEmpty(APIAdoptions.SwaggerConfiguration.HostPostfix)
                            ? string.Empty
                            : "/" + APIAdoptions.SwaggerConfiguration.HostPostfix;
    swaggerUI.SwaggerEndpoint($"{hostPrefix}/{APIAdoptions.SwaggerConfiguration.PrefixDocPath}/{APIAdoptions.SwaggerConfiguration.Version}/swagger.json", APIAdoptions.SwaggerConfiguration.Title);
    swaggerUI.RoutePrefix = APIAdoptions.SwaggerConfiguration.PrefixDocPath;
    swaggerUI.EnableTryItOutByDefault();
});

app.Run();