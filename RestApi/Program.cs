using HEAppE.BackgroundThread;
using HEAppE.BusinessLogicTier.Configuration;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace HEAppE.RestApi
{
    public class Program
    {
        static MiddlewareBackgroundTaskRunner timer;

        public static void Main(string[] args)
        {
            //Initialize configuration to use it in middleware background threads
            string localRunEnv = Environment.GetEnvironmentVariable("ASPNETCORE_RUNTYPE_ENVIRONMENT");
            IConfigurationRoot config;
            if (localRunEnv == "Docker")
            {
                config = new ConfigurationBuilder()
                            .AddJsonFile("/opt/heappe/confs/appsettings.json", false, true)
                                .Build();
            }
            else
            {
                config = new ConfigurationBuilder()
                            .AddJsonFile("C:/Heappe/projects/develop/app/confs/appsettings.json", false, true)
                                .Build();
            }
            config.Bind("BusinessLogicSettings", new BusinessLogicConfiguration());

            timer = new MiddlewareBackgroundTaskRunner();
            timer.Start();

            CreateWebHostBuilder(args).Build().Run();
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args)
        {
            IWebHostBuilder builder;
            string localRunEnv = Environment.GetEnvironmentVariable("ASPNETCORE_RUNTYPE_ENVIRONMENT");
            if (localRunEnv == "Docker")
            {
                // Docker run
                builder = WebHost.CreateDefaultBuilder()
                    .UseUrls("http://*:80") //For docker
                    .ConfigureAppConfiguration((hostingContext, config) =>
                    {
                        config.AddJsonFile("/opt/heappe/confs/appsettings.json", false, true);
                        config.AddNotJson("/opt/heappe/confs/seed.njson");
                    })
                    .UseKestrel()
                    .UseStartup<Startup>();
            }
            else
            {
                // Run w/o docker
                builder = WebHost.CreateDefaultBuilder()
                    .UseUrls("http://*:5000;https://*:5001")
                    .ConfigureAppConfiguration((hostingContext, config) =>
                    {
                        config.AddJsonFile("C:/Heappe/projects/develop/app/confs/appsettings.json", false, true);
                        config.AddNotJson("C:/Heappe/projects/develop/app/confs/seed.njson");
                    })
                    //.UseKestrel()
                    //.UseIISIntegration()
                    .UseStartup<Startup>();
            }
            return builder;
        }
    }
    #region NJSON Parser Methods
    public static class ConfigurationExtensionMethods
    {
        public static IConfigurationBuilder AddNotJson(this IConfigurationBuilder builder, string path)
        {
            var releaseConfigJson = File.ReadAllText(path);
            releaseConfigJson = releaseConfigJson.Replace(@"\", @"\\");

            var dictionary = JsonConfigurationParser.Parse(releaseConfigJson);
            return builder.AddInMemoryCollection(dictionary);
        }
    }

    public class JsonConfigurationParser
    {
        private JsonConfigurationParser() { }

        private readonly IDictionary<string, string> _data = new SortedDictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private readonly Stack<string> _context = new Stack<string>();
        private string _currentPath;

        public static IDictionary<string, string> Parse(string json) => new JsonConfigurationParser().ParseJson(json);

        private IDictionary<string, string> ParseJson(string json)
        {
            _data.Clear();

            var jsonConfig = JObject.Parse(json);

            VisitJObject(jsonConfig);

            return _data;
        }

        private void VisitJObject(JObject jObject)
        {
            foreach (var property in jObject.Properties())
            {
                EnterContext(property.Name);
                VisitProperty(property);
                ExitContext();
            }
        }

        private void VisitProperty(JProperty property)
        {
            VisitToken(property.Value);
        }

        private void VisitToken(JToken token)
        {
            switch (token.Type)
            {
                case JTokenType.Object:
                    VisitJObject(token.Value<JObject>());
                    break;

                case JTokenType.Array:
                    VisitArray(token.Value<JArray>());
                    break;

                case JTokenType.Integer:
                case JTokenType.Float:
                case JTokenType.String:
                case JTokenType.Boolean:
                case JTokenType.Bytes:
                case JTokenType.Raw:
                case JTokenType.Null:
                    VisitPrimitive(token.Value<JValue>());
                    break;

                default:
                    throw new FormatException("Unsupported JSON token");
            }
        }

        private void VisitArray(JArray array)
        {
            for (int index = 0; index < array.Count; index++)
            {
                EnterContext(index.ToString());
                VisitToken(array[index]);
                ExitContext();
            }
        }

        private void VisitPrimitive(JValue data)
        {
            var key = _currentPath;

            if (_data.ContainsKey(key))
            {
                throw new FormatException("Duplicate Key");
            }

            _data[key] = data.Value != null 
                            ? data.ToString(CultureInfo.InvariantCulture) 
                            : null;
        }

        private void EnterContext(string context)
        {
            _context.Push(context);
            _currentPath = ConfigurationPath.Combine(_context.Reverse());
        }

        private void ExitContext()
        {
            _context.Pop();
            _currentPath = ConfigurationPath.Combine(_context.Reverse());
        }
        #endregion
    }
}
