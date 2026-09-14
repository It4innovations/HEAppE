using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Memory;
using Xunit;
using HEAppE.HpcConnectionFramework.Configuration;

namespace SshCaAPITests;

public class ClusterRuntimeConfigurationTests
{
    [Fact]
    public void ClusterRuntimeConfiguration_WithEmptyBooleanStrings_DoesNotThrowAndDefaultsToFalse()
    {
        var customConfig = new Dictionary<string, string>
        {
            { "EnableCallback", "" },
            { "EnableGracefulTimeout", "   " },
            { "SyncScriptsViaSftp", "" }
        };

        var config = ClusterRuntimeConfiguration.For(customConfig);

        Assert.False(config.EnableCallback);
        Assert.False(config.EnableGracefulTimeout);
        Assert.False(config.SyncScriptsViaSftp);
    }

    [Fact]
    public void ClusterRuntimeConfiguration_WithFullKeyPathsAndEmptyBooleans_DoesNotThrowAndDefaultsToFalse()
    {
        var customConfig = new Dictionary<string, string>
        {
            { "HPCConnectionFrameworkSettings:ScriptsSettings:EnableCallback", "" },
            { "HPCConnectionFrameworkSettings:ScriptsSettings:EnableGracefulTimeout", "" },
            { "HPCConnectionFrameworkSettings:ScriptsSettings:SyncScriptsViaSftp", "" }
        };

        var config = ClusterRuntimeConfiguration.For(customConfig);

        Assert.False(config.EnableCallback);
        Assert.False(config.EnableGracefulTimeout);
        Assert.False(config.SyncScriptsViaSftp);
    }

    [Fact]
    public void ClusterRuntimeConfiguration_WithTrueBooleanStrings_ParsesAsTrue()
    {
        var customConfig = new Dictionary<string, string>
        {
            { "EnableCallback", "true" },
            { "EnableGracefulTimeout", "true" },
            { "SyncScriptsViaSftp", "true" }
        };

        var config = ClusterRuntimeConfiguration.For(customConfig);

        Assert.True(config.EnableCallback);
        Assert.True(config.EnableGracefulTimeout);
        Assert.True(config.SyncScriptsViaSftp);
    }

    [Fact]
    public void ClusterRuntimeConfiguration_WithEmptyCallbackUrl_FallsBackToGlobalDefault()
    {
        HPCConnectionFrameworkConfiguration.ScriptsSettings.CallbackUrl = "https://global.heappe.eu/callback";

        var customConfig = new Dictionary<string, string>
        {
            { "CallbackUrl", "" }
        };

        var config = ClusterRuntimeConfiguration.For(customConfig);

        Assert.Equal("https://global.heappe.eu/callback", config.Scripts.CallbackUrl);
    }

    [Fact]
    public void ClusterRuntimeConfiguration_WithSpecificCallbackUrl_UsesSpecifiedValue()
    {
        HPCConnectionFrameworkConfiguration.ScriptsSettings.CallbackUrl = "https://global.heappe.eu/callback";

        var customConfig = new Dictionary<string, string>
        {
            { "CallbackUrl", "https://cluster-specific.heappe.eu/callback" }
        };

        var config = ClusterRuntimeConfiguration.For(customConfig);

        Assert.Equal("https://cluster-specific.heappe.eu/callback", config.Scripts.CallbackUrl);
    }

    [Fact]
    public void ClusterRuntimeConfiguration_ForNullOrEmpty_ReturnsSameDefaultInstance()
    {
        var config1 = ClusterRuntimeConfiguration.For(null);
        var config2 = ClusterRuntimeConfiguration.For(new Dictionary<string, string>());

        Assert.Same(config1, config2);
    }

    [Fact]
    public void ClusterRuntimeConfiguration_ForIdenticalDictionaries_ReturnsCachedInstance()
    {
        var dict1 = new Dictionary<string, string>
        {
            { "EnableCallback", "true" },
            { "CallbackUrl", "https://test.eu" }
        };
        var dict2 = new Dictionary<string, string>
        {
            { "CallbackUrl", "https://test.eu" },
            { "EnableCallback", "true" }
        };

        var config1 = ClusterRuntimeConfiguration.For(dict1);
        var config2 = ClusterRuntimeConfiguration.For(dict2);

        Assert.Same(config1, config2);
    }

    [Fact]
    public void ClusterRuntimeConfiguration_ResetCache_ClearsCache()
    {
        var dict = new Dictionary<string, string>
        {
            { "EnableCallback", "true" }
        };

        var config1 = ClusterRuntimeConfiguration.For(dict);
        ClusterRuntimeConfiguration.ResetCache();
        var config2 = ClusterRuntimeConfiguration.For(dict);

        Assert.NotSame(config1, config2);
    }

    [Fact]
    public void ClusterRuntimeConfiguration_GetValue_FallsBackToGlobalConfiguration()
    {
        var globalConfig = new ConfigurationBuilder()
            .Add(new MemoryConfigurationSource
            {
                InitialData = new Dictionary<string, string>
                {
                    { "SomeGlobalSetting", "GlobalVal" },
                    { "OverriddenSetting", "GlobalVal2" }
                }
            })
            .Build();

        ClusterRuntimeConfiguration.GlobalConfiguration = globalConfig;

        var customConfig = new Dictionary<string, string>
        {
            { "OverriddenSetting", "ClusterVal" }
        };

        var config = ClusterRuntimeConfiguration.For(customConfig);

        Assert.Equal("GlobalVal", config.GetValue("SomeGlobalSetting"));
        Assert.Equal("ClusterVal", config.GetValue("OverriddenSetting"));
    }
}
