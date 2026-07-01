using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using HEAppE.DomainObjects.ClusterInformation;
using HEAppE.Exceptions.Internal;

namespace HEAppE.HpcConnectionFramework.SystemConnectors.SSH;

/// <summary>
/// Helper to handle bootstrapping and merging of krb5.conf configuration.
/// </summary>
public static class KerberosConfigHelper
{
    internal static readonly System.Threading.SemaphoreSlim _krbConfigSemaphore = new System.Threading.SemaphoreSlim(1, 1);
    private const string DestPath = "/opt/heappe/confs/krb5.conf";

    /// <summary>
    /// Verifies that the krb5.conf file exists either in the path specified by the KRB5_CONFIG env variable,
    /// at the default HEAppE path, or at the system-wide /etc/krb5.conf path.
    /// </summary>
    public static void VerifyKrb5ConfigExists()
    {
        string envPath = Environment.GetEnvironmentVariable("KRB5_CONFIG");
        if (!string.IsNullOrEmpty(envPath))
        {
            if (!File.Exists(envPath))
            {
                throw new ClusterAuthenticationException("MissingKrb5Config");
            }
            return;
        }

        if (!File.Exists(DestPath) && !File.Exists("/etc/krb5.conf"))
        {
            throw new ClusterAuthenticationException("MissingKrb5Config");
        }
    }

    /// <summary>
    /// Bootstraps/updates the krb5.conf file for a specific connection target.
    /// </summary>
    public static async Task BootstrapConfigIfNeededAsync(string masterNodeName, Cluster cluster, ILogger logger)
    {
        bool needsBootstrap = false;
        try
        {
            if (!File.Exists(DestPath))
            {
                needsBootstrap = true;
            }
        }
        catch
        {
            needsBootstrap = true;
        }

        if (needsBootstrap)
        {
            await _krbConfigSemaphore.WaitAsync();
            try
            {
                if (!File.Exists(DestPath))
                {
                    logger.LogInformation("Bootstrapping krb5.conf for Kerberos connection...");
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
                    sb.AppendLine("");
                    sb.AppendLine("[domain_realm]");
                    sb.AppendLine($"    .{domain} = {realm}");
                    sb.AppendLine($"    {domain} = {realm}");

                    var dir = Path.GetDirectoryName(DestPath);
                    if (dir != null && !Directory.Exists(dir))
                    {
                        Directory.CreateDirectory(dir);
                    }

                    string existingContent = "";
                    if (File.Exists(DestPath))
                    {
                        try { existingContent = await File.ReadAllTextAsync(DestPath); } catch { }
                    }
                    string mergedContent = Krb5ConfigMerger.Merge(existingContent, sb.ToString());

                    string tempPath = DestPath + ".tmp";
                    await File.WriteAllTextAsync(tempPath, mergedContent);
                    File.Move(tempPath, DestPath, overwrite: true);
                    logger.LogInformation($"Successfully bootstrapped krb5.conf at {DestPath}");
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to bootstrap krb5.conf");
            }
            finally
            {
                _krbConfigSemaphore.Release();
            }
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
