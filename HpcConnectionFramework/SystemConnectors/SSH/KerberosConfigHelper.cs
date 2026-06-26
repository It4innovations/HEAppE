using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using HEAppE.DomainObjects.ClusterInformation;

namespace HEAppE.HpcConnectionFramework.SystemConnectors.SSH;

/// <summary>
/// Helper to handle bootstrapping and merging of krb5.conf configuration.
/// </summary>
public static class KerberosConfigHelper
{
    internal static readonly System.Threading.SemaphoreSlim _krbConfigSemaphore = new System.Threading.SemaphoreSlim(1, 1);
    private const string DestPath = "/opt/heappe/confs/krb5.conf";

    private static string GetConfigFilePath()
    {
        if (Directory.Exists(DestPath))
        {
            return Path.Combine(DestPath, "krb5.conf");
        }
        return DestPath;
    }

    /// <summary>
    /// Bootstraps/updates the krb5.conf file for a specific connection target.
    /// </summary>
    public static async Task BootstrapConfigIfNeededAsync(string masterNodeName, Cluster cluster, ILogger logger)
    {
        string targetPath = GetConfigFilePath();

        // 1. If a system-wide krb5.conf already exists, do not bootstrap and let the library use it.
        if (File.Exists("/etc/krb5.conf"))
        {
            if (File.Exists(targetPath))
            {
                try
                {
                    File.Delete(targetPath);
                    logger.LogInformation("Deleted bootstrapped krb5.conf to fall back to system /etc/krb5.conf");
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Failed to delete bootstrapped krb5.conf");
                }
            }
            return;
        }

        // 2. Otherwise, bootstrap or update the bootstrapped config file.
        await _krbConfigSemaphore.WaitAsync();
        try
        {
            string existingContent = "";
            if (File.Exists(targetPath))
            {
                try
                {
                    existingContent = await File.ReadAllTextAsync(targetPath);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Failed to read existing krb5.conf content");
                }
            }

            string domain = !string.IsNullOrEmpty(cluster.DomainName) 
                ? cluster.DomainName 
                : GetDomainFromHostname(masterNodeName);
            string realm = domain.ToUpperInvariant();
            string kdc = masterNodeName;

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("[libdefaults]");
            sb.AppendLine($"    default_realm = {realm}");
            sb.AppendLine("    dns_lookup_realm = false");
            sb.AppendLine("    dns_lookup_kdc = false");
            sb.AppendLine("    rdns = false");
            sb.AppendLine("    ticket_lifetime = 24h");
            sb.AppendLine("    forwardable = true");
            sb.AppendLine("");
            sb.AppendLine("[realms]");
            sb.AppendLine($"    {realm} = {{");
            sb.AppendLine($"        kdc = {kdc}:88");
            sb.AppendLine($"        admin_server = {kdc}");
            sb.AppendLine("    }");

            // Check if there are custom Kerberos settings in Cluster.CustomConfiguration
            string customRealm = null;
            if (cluster.CustomConfiguration != null &&
                cluster.CustomConfiguration.TryGetValue("KerberosRealm", out customRealm) &&
                !string.IsNullOrEmpty(customRealm))
            {
                sb.AppendLine($"    {customRealm} = {{");
                if (cluster.CustomConfiguration.TryGetValue("KerberosKdc", out var customKdc) && !string.IsNullOrEmpty(customKdc))
                {
                    var kdcs = customKdc.Split(',');
                    foreach (var k in kdcs)
                    {
                        var trimmedKdc = k.Trim();
                        if (!string.IsNullOrEmpty(trimmedKdc))
                        {
                            sb.AppendLine($"        kdc = {trimmedKdc}");
                        }
                    }
                }
                else
                {
                    // Fallback KDC to the masterNodeName if none specified
                    sb.AppendLine($"        kdc = {kdc}:88");
                }

                if (cluster.CustomConfiguration.TryGetValue("KerberosAdminServer", out var adminServer) && !string.IsNullOrEmpty(adminServer))
                {
                    sb.AppendLine($"        admin_server = {adminServer}");
                }
                sb.AppendLine("    }");
            }

            sb.AppendLine("");
            sb.AppendLine("[domain_realm]");
            // If we have a custom realm, map both the default domain and optionally custom domain mappings to it
            if (!string.IsNullOrEmpty(customRealm))
            {
                sb.AppendLine($"    .{domain} = {customRealm}");
                sb.AppendLine($"    {domain} = {customRealm}");

                if (cluster.CustomConfiguration.TryGetValue("KerberosDomainMapping", out var customDomainMap) && !string.IsNullOrEmpty(customDomainMap))
                {
                    var domains = customDomainMap.Split(',');
                    foreach (var d in domains)
                    {
                        var trimmedDomain = d.Trim();
                        if (!string.IsNullOrEmpty(trimmedDomain))
                        {
                            // Ensure leading dot if mapped as domain wildcard
                            var dotDomain = trimmedDomain.StartsWith('.') ? trimmedDomain : $".{trimmedDomain}";
                            sb.AppendLine($"    {dotDomain} = {customRealm}");
                            sb.AppendLine($"    {trimmedDomain} = {customRealm}");
                        }
                    }
                }
            }
            else
            {
                sb.AppendLine($"    .{domain} = {realm}");
                sb.AppendLine($"    {domain} = {realm}");
            }

            string mergedContent = Krb5ConfigMerger.Merge(existingContent, sb.ToString());

            string normalizedExisting = existingContent.Replace("\r\n", "\n").Trim();
            string normalizedMerged = mergedContent.Replace("\r\n", "\n").Trim();

            if (string.IsNullOrEmpty(normalizedExisting) || normalizedExisting != normalizedMerged)
            {
                logger.LogInformation("Updating/Bootstrapping krb5.conf for Kerberos connection...");
                var dir = Path.GetDirectoryName(targetPath);
                if (dir != null && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                string tempPath = targetPath + ".tmp";
                await File.WriteAllTextAsync(tempPath, mergedContent);
                File.Move(tempPath, targetPath, overwrite: true);
                logger.LogInformation($"Successfully updated krb5.conf at {targetPath}");
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to bootstrap/update krb5.conf");
        }
        finally
        {
            _krbConfigSemaphore.Release();
        }
    }

    private static string GetDomainFromHostname(string hostname)
    {
        if (string.IsNullOrEmpty(hostname)) return "local";
        int firstDot = hostname.IndexOf('.');
        if (firstDot > 0 && firstDot < hostname.Length - 1)
        {
            return hostname.Substring(firstDot + 1);
        }
        return hostname;
    }
}
