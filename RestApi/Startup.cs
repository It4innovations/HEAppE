using AspNetCoreRateLimit;
using HEAppE.BackgroundThread.Configuration;
using HEAppE.BusinessLogicTier.Configuration;
using HEAppE.BusinessLogicTier.Factory;
using HEAppE.CertificateGenerator.Configuration;
using HEAppE.DataAccessTier;
using HEAppE.ExternalAuthentication.Configuration;
using HEAppE.FileTransferFramework;
using HEAppE.HpcConnectionFramework.Configuration;
using HEAppE.OpenStackAPI.Configuration;
using HEAppE.RestApi.Configuration;
using log4net;
using MicroKnights.Log4NetHelper;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Rewrite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using HEAppE.DataAccessTier.Configuration;

namespace HEAppE.RestApi
{
    /// <summary>
    /// Startup
    /// </summary>
    public class Startup
    {
        #region Instances
        /// <summary>
        /// Specific origins
        /// </summary>
        private readonly string _allowSpecificOrigins = "HEAppEDefaultOrigins";
        #endregion
        #region Properties       
        /// <summary>
        /// Configuration property
        /// </summary>
        public IConfiguration Configuration { get; }

        #endregion
        #region Constructors
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="configuration">Iconfiguration</param>
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }
        #endregion
        #region Methods
        /// <summary>
        /// Configure Services
        /// </summary>
        /// <param name="services">Collection services</param>
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMemoryCache();

            //IP rate limitation
            services.Configure<IpRateLimitOptions>(Configuration.GetSection("IpRateLimiting"));
            services.Configure<IpRateLimitPolicies>(Configuration.GetSection("IpRateLimitPolicies"));

            //Other configuration

            Configuration.Bind("BackGroundThreadSettings", new BackGroundThreadConfiguration());
            Configuration.Bind("BusinessLogicSettings", new BusinessLogicConfiguration());
            Configuration.Bind("CertificateGeneratorSettings", new CertificateGeneratorConfiguration());
            Configuration.Bind("MiddlewareContextSettings", new MiddlewareContextSettings());
            MiddlewareContextSettings.ConnectionString = Configuration.GetConnectionString("MiddlewareContext");
            Configuration.Bind("DatabaseMigrationSettings", new DatabaseMigrationSettings());
            Configuration.Bind("HPCConnectionFrameworkSettings", new HPCConnectionFrameworkConfiguration());
            Configuration.Bind("ApplicationAPISettings", new ApplicationAPIConfiguration());
            Configuration.Bind("ExternalAuthenticationSettings", new ExternalAuthConfiguration());
            Configuration.Bind("OpenStackSettings", new OpenStackSettings());

            services.AddSingleton<IIpPolicyStore, MemoryCacheIpPolicyStore>();
            services.AddSingleton<IRateLimitCounterStore, MemoryCacheRateLimitCounterStore>();
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();
            services.AddSingleton<IProcessingStrategy, AsyncKeyLockProcessingStrategy>();

            //UserOrgHttpClient
            //services.AddOptions<ExternalAuthConfiguration>().BindConfiguration("ExternalAuthenticationSettings");

            services.AddHttpClient("userOrgApi", conf =>
            {
                if (!string.IsNullOrEmpty(LexisAuthenticationConfiguration.BaseAddress))
                {
                    conf.BaseAddress = new Uri(LexisAuthenticationConfiguration.BaseAddress);
                }
            });

            //CORS
            services.AddCors(options =>
            {
                options.AddPolicy(name: _allowSpecificOrigins, builder =>
                {
                    builder.WithOrigins(ApplicationAPIConfiguration.AllowedHosts)
                            .AllowAnyHeader()
                            .AllowAnyMethod();
                });
            });

            services.AddControllers().AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.PropertyNamingPolicy = null;
            });

            //SwaggerGen
            services.AddSwaggerGen(gen =>
            {
                // Default Swagger document
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

                // Swagger document for hidden/private API
                gen.SwaggerDoc("DetailedJobReporting", new OpenApiInfo
                {
                    Version = SwaggerConfiguration.Version,
                    Title = SwaggerConfiguration.DetailedJobReportingTitle,
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

                // Group APIs into documents based on ApiExplorerSettings
                gen.DocInclusionPredicate((documentName, apiDescription) =>
                {
                    // Include in the private document if tagged with DetailedJobReporting
                    if (documentName == "DetailedJobReporting")
                    {
                        return apiDescription.GroupName == "DetailedJobReporting";
                    }

                    // Include in the public document
                    if (documentName == SwaggerConfiguration.Version)
                    {
                        return string.IsNullOrEmpty(apiDescription.GroupName);
                    }

                    return false;
                });

                var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                gen.IncludeXmlComments(xmlPath);
            });
            services.AddRazorPages();
            //Localization and resources
            services.AddLocalization();
            services.Configure<RequestLocalizationOptions>(options =>
            {
                var supportedCultures = new List<CultureInfo>
                {
                    new CultureInfo("en"),
                    new CultureInfo("cs")
                };

                options.DefaultRequestCulture = new RequestCulture("en");
                options.SupportedCultures = supportedCultures;
            });
        }

        /// <summary>
        /// Configure service
        /// </summary>
        /// <param name="app">Application</param>
        /// <param name="env">Enviroment</param>
        /// <param name="loggerFactory">Logger factory</param>
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ILoggerFactory loggerFactory)
        {
            LogicFactory.ServiceProvider = app.ApplicationServices;
            var logRepository = LogManager.GetRepository(Assembly.GetEntryAssembly());
            GlobalContext.Properties["instanceName"] = DeploymentInformationsConfiguration.Name;
            GlobalContext.Properties["instanceVersion"] = DeploymentInformationsConfiguration.Version;
            GlobalContext.Properties["ip"] = DeploymentInformationsConfiguration.DeployedIPAddress;

            if (Environment.GetEnvironmentVariable("ASPNETCORE_RUNTYPE_ENVIRONMENT") == "Docker")
            {
                loggerFactory.AddLog4Net("Logging/log4netDocker.config");
            }
            else
            {
                loggerFactory.AddLog4Net("Logging/log4net.config");
            }

            AdoNetAppenderHelper.SetConnectionString(Configuration.GetConnectionString("Logging"));


            ServiceActivator.Configure(app.ApplicationServices);
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseIpRateLimiting();

            app.UseStatusCodePages();
            app.UseStaticFiles();
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
                swaggerUI.SwaggerEndpoint($"{hostPrefix}/{SwaggerConfiguration.PrefixDocPath}/DetailedJobReporting/swagger.json", SwaggerConfiguration.DetailedJobReportingTitle);
                swaggerUI.RoutePrefix = SwaggerConfiguration.PrefixDocPath;
                swaggerUI.EnableTryItOutByDefault();
            });

            app.UseRequestLocalization();

            app.UseRouting();
            app.UseMiddleware<ExceptionMiddleware>();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
                endpoints.MapRazorPages();
            });

            app.UseCors(_allowSpecificOrigins);

            var option = new RewriteOptions();
            option.AddRedirect("^$", $"{SwaggerConfiguration.HostPostfix}/swagger/index.html");
            app.UseRewriter(option);
        }
        #endregion
    }
}