using FluentValidation;
using HEAppE.DataAccessTier;
using HEAppE.DataStagingAPI;
using HEAppE.DataStagingAPI.API.AbstractTypes;
using HEAppE.DataStagingAPI.Configuration;
using HEAppE.ExtModels.General.Models;
using Microsoft.OpenApi.Models;
using System.Configuration;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

if (Environment.GetEnvironmentVariable("ASPNETCORE_RUNTYPE_ENVIRONMENT") == "Docker")
{
    //builder.Logging.AddLog4Net("log4netDocker.config");
    builder.Configuration.AddJsonFile("/opt/heappe/confs/seed.njson");
    builder.Configuration.AddJsonFile("/opt/heappe/confs/appsettings-data.json", false, false);
}
else
{
    //builder.Logging.AddLog4Net("log4net.config");
}

// Configurations
builder.Configuration.Bind("ApplicationAPISettings", new ApplicationAPIConfiguration());


//TODO Need to be rewritten into DI
builder.Configuration.Bind("MiddlewareContextSettings", new MiddlewareContextSettings());
MiddlewareContextSettings.ConnectionString = builder.Configuration.GetConnectionString("MiddlewareContext");


// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddSwaggerGen(gen =>
{
    gen.SwaggerDoc(SwaggerConfiguration.Version, new OpenApiInfo
    {
        Version = SwaggerConfiguration.Version,
        Title = SwaggerConfiguration.Title,
        Description = SwaggerConfiguration.Description,
        TermsOfService = new Uri(SwaggerConfiguration.TermOfUsageUrl),
        License = new OpenApiLicense()
        {
            Name = SwaggerConfiguration.License,
            Url = new Uri(SwaggerConfiguration.LicenseUrl),
        },
        Contact = new OpenApiContact()
        {
            Name = SwaggerConfiguration.ContactName,
            Email = SwaggerConfiguration.ContactEmail,
            Url = new Uri(SwaggerConfiguration.ContactUrl),
        }
    });
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

app.UseMiddleware<ExceptionMiddleware>();
app.UseStatusCodePages();
app.RegisterApiRoutes();

app.UseSwagger(swagger =>
{
    swagger.PreSerializeFilters.Add((swaggerDoc, httpReq) =>
    {
        swaggerDoc.Servers = new List<OpenApiServer> {
                        new OpenApiServer{
                            Url =  $"{SwaggerConfiguration.Host}/{SwaggerConfiguration.HostPostfix}"
                        }
                    };
    });
    swagger.RouteTemplate = $"/{SwaggerConfiguration.PrefixDocPath}/{{documentname}}/swagger.json";
});

app.UseSwaggerUI(swaggerUI =>
{
    string hostPrefix = string.IsNullOrEmpty(SwaggerConfiguration.HostPostfix)
                            ? string.Empty
                            : "/" + SwaggerConfiguration.HostPostfix;
    swaggerUI.SwaggerEndpoint($"{hostPrefix}/{SwaggerConfiguration.PrefixDocPath}/{SwaggerConfiguration.Version}/swagger.json", SwaggerConfiguration.Title);
    swaggerUI.RoutePrefix = SwaggerConfiguration.PrefixDocPath;
    swaggerUI.EnableTryItOutByDefault();
});

app.Run();