using System.Collections.Generic;
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
}
