using FluentValidation;
using HEAppE.DataAccessTier;
using HEAppE.DataStagingAPI;
using HEAppE.DataStagingAPI.API.AbstractTypes;
using HEAppE.DataStagingAPI.Configuration;
using HEAppE.ExtModels.General.Models;
using HEAppE.FileTransferFramework;
using Microsoft.OpenApi.Models;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

if (Environment.GetEnvironmentVariable("ASPNETCORE_RUNTYPE_ENVIRONMENT") == "Docker")
{
    builder.Logging.AddLog4Net("log4netDocker.config");
    //TODO in different way
    builder.Configuration.AddJsonFile("/opt/heappe/confs/seed.njson");
    builder.Configuration.AddJsonFile("/opt/heappe/confs/appsettings-data.json", false, false);
}
else
{
    builder.Logging.AddLog4Net("log4net.config");
}

// Configurations
// TODO Validation of configurations
builder.Services.AddOptions<ApplicationAPIOptions>().BindConfiguration("ApplicationAPIConfiguration");
//                    .ValidateDataAnnotations()
//                    .ValidateOnStart();

var options = new ApplicationAPIOptions();
builder.Configuration.GetSection("ApplicationAPIConfiguration").Bind(options);

builder.Configuration.Bind("MiddlewareContextSettings", new MiddlewareContextSettings());
MiddlewareContextSettings.ConnectionString = builder.Configuration.GetConnectionString("MiddlewareContext");

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddSwaggerGen(gen =>
{
    gen.SwaggerDoc(options.SwaggerConfiguration.Version, new OpenApiInfo
    {
        Version = options.SwaggerConfiguration.Version,
        Title = options.SwaggerConfiguration.Title,
        Description = options.SwaggerConfiguration.Description,
        TermsOfService = new Uri(options.SwaggerConfiguration.TermOfUsageUrl),
        License = new OpenApiLicense()
        {
            Name = options.SwaggerConfiguration.License,
            Url = new Uri(options.SwaggerConfiguration.LicenseUrl),
        },
        Contact = new OpenApiContact()
        {
            Name = options.SwaggerConfiguration.ContactName,
            Email = options.SwaggerConfiguration.ContactEmail,
            Url = new Uri(options.SwaggerConfiguration.ContactUrl),
        }
    });
    gen.AddSecurityDefinition(options.AuthenticationParamHeaderName, new OpenApiSecurityScheme
    {
        Description = $"{options.AuthenticationParamHeaderName} must appear in header",
        Type = SecuritySchemeType.ApiKey,
        Name = options.AuthenticationParamHeaderName,
        In = ParameterLocation.Header,
        Scheme = $"{options.AuthenticationParamHeaderName}Scheme"
    });
    var key = new OpenApiSecurityScheme()
    {
        Reference = new OpenApiReference
        {
            Type = ReferenceType.SecurityScheme,
            Id = options.AuthenticationParamHeaderName
        },
        In = ParameterLocation.Header
    };
    var requirement = new OpenApiSecurityRequirement
        {
            { key, new List<string>() }
        };
    gen.AddSecurityRequirement(requirement);
});

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
    options.SerializerOptions.PropertyNamingPolicy = null;
    options.SerializerOptions.PropertyNameCaseInsensitive = true;
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddValidatorsFromAssemblyContaining<Program>(ServiceLifetime.Singleton);
builder.Services.AddTransient<IValidator<AuthorizedSubmittedJobIdModel>, AuthorizedSubmittedJobIdModelValidator>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

ServiceActivator.Configure(app.Services);
//app.UseCors();
app.UseMiddleware<ExceptionMiddleware>();
app.UseStatusCodePages();
app.RegisterApiRoutes();

app.UseSwagger(swagger =>
{
    swagger.PreSerializeFilters.Add((swaggerDoc, httpReq) =>
    {
        swaggerDoc.Servers = new List<OpenApiServer> {
                        new OpenApiServer{
                            Url =  $"{options.SwaggerConfiguration.Host}/{options.SwaggerConfiguration.HostPostfix}"
                        }
                    };
    });
    swagger.RouteTemplate = $"/{options.SwaggerConfiguration.PrefixDocPath}/{{documentname}}/swagger.json";
});

app.UseSwaggerUI(swaggerUI =>
{
    string hostPrefix = string.IsNullOrEmpty(options.SwaggerConfiguration.HostPostfix)
                            ? string.Empty
                            : "/" + options.SwaggerConfiguration.HostPostfix;
    swaggerUI.SwaggerEndpoint($"{hostPrefix}/{options.SwaggerConfiguration.PrefixDocPath}/{options.SwaggerConfiguration.Version}/swagger.json", options.SwaggerConfiguration.Title);
    swaggerUI.RoutePrefix = options.SwaggerConfiguration.PrefixDocPath;
    swaggerUI.EnableTryItOutByDefault();
});

app.Run();