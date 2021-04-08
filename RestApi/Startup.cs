using HEAppE.DataAccessTier;
using HEAppE.KeycloakOpenIdAuthentication;
using log4net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Rewrite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.OpenApi.Models;
using HEAppE.RestApi.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using HEAppE.OpenStackAPI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using AspNetCoreRateLimit;
using Microsoft.AspNetCore.Http;
using HEAppE.FileTransferFramework;

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
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Latest);

            //IP rate limitation
            services.AddMemoryCache();

            services.Configure<IpRateLimitOptions>(Configuration.GetSection("IpRateLimiting"));      
            services.Configure<IpRateLimitPolicies>(Configuration.GetSection("IpRateLimitPolicies"));

            services.AddSingleton<IIpPolicyStore, MemoryCacheIpPolicyStore>();
            services.AddSingleton<IRateLimitCounterStore, MemoryCacheRateLimitCounterStore>();
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();

            //Other configuration
            var middlewareContextSettings = new MiddlewareContextSettings();
            Configuration.Bind("MiddlewareContextSettings", middlewareContextSettings);
            Configuration.Bind("ApplicationAPISettings", new ApplicationAPIConfiguration());
            Configuration.Bind("KeycloakSettings", new KeycloakSettings());
            Configuration.Bind("OpenStackSettings", new OpenStackSettings());

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

                var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                gen.IncludeXmlComments(xmlPath);
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
            var logRepository = LogManager.GetRepository(Assembly.GetEntryAssembly());
            if (Environment.GetEnvironmentVariable("ASPNETCORE_RUNTYPE_ENVIRONMENT") == "Docker")
            {
                loggerFactory.AddLog4Net("log4netDocker.config");
            }
            else
            {
                loggerFactory.AddLog4Net("log4net.config");
            }

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
                swaggerUI.RoutePrefix = SwaggerConfiguration.PrefixDocPath;
            });

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
