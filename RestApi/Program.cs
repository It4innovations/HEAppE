using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;

namespace HEAppE.RestApi;

public class Program
{
    public static void Main(string[] args)
    {
        var host = CreateWebHostBuilder(args).Build();
        host.Run();
    }

    public static IWebHostBuilder CreateWebHostBuilder(string[] args)
    {
        IWebHostBuilder builder;
        var localRunEnv = Environment.GetEnvironmentVariable("ASPNETCORE_RUNTYPE_ENVIRONMENT");
        if (localRunEnv == "Docker")
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
        else
            // Run w/o docker
            builder = WebHost.CreateDefaultBuilder()
                .UseUrls("http://*:5000")
                .ConfigureAppConfiguration((hostingContext, config) =>
                {
                    config.AddJsonFile("P:\\source\\localHEAppE\\confs/appsettings.json", false, true);
                    config.AddNotJson("P:\\source\\localHEAppE\\confs/seed.njson");
                })
                .UseStartup<Startup>();
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
    private JsonConfigurationParser()
    {
    }

    private readonly IDictionary<string, string> _data =
        new SortedDictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    private readonly Stack<string> _context = new();
    private string _currentPath;

    public static IDictionary<string, string> Parse(string json)
    {
        return new JsonConfigurationParser().ParseJson(json);
    }

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
        for (var index = 0; index < array.Count; index++)
        {
            EnterContext(index.ToString());
            VisitToken(array[index]);
            ExitContext();
        }
    }

    private void VisitPrimitive(JValue data)
    {
        var key = _currentPath;

        if (_data.ContainsKey(key)) throw new FormatException("Duplicate Key");

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