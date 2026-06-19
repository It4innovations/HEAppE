namespace HEAppE.DataStagingAPI.Configuration;
#nullable disable
/// <summary>
///     Swagger setting from config
/// </summary>
public sealed class SwaggerOptions
{
    #region Instances

    /// <summary>
    ///     Host address with schema
    /// </summary>
    private string _host;

    /// <summary>
    ///     Host postfix address
    /// </summary>
    private string _hostPostfix;

    /// <summary>
    ///     Swagger prefix
    /// </summary>
    private string _prefixDocPath;

    #endregion

    #region Properties

    /// <summary>
    ///     API Version
    /// </summary>
    public string Version { get; set; } = "v1.0.0";
    public string Title { get; set; } = "Data-Staging API";
    public string Description { get; set; } = "Data-Staging API";
    public string Host { get; set; } = "http://localhost:5001";
    public string HostPostfix { get; set; } = "";
    public string PrefixDocPath { get; set; } = "swagger";
    public string TermOfUsageUrl { get; set; } = "https://twitter.com/it4innovations";
    public string ContactName { get; set; } = "IT4Innovations";
    public string ContactEmail { get; set; } = "support.heappe@it4i.cz";
    public string ContactUrl { get; set; } = "https://twitter.com/it4innovations";
    public string License { get; set; } = "GNU General Public License v3.0";
    public string LicenseUrl { get; set; } = "https://www.gnu.org/licenses/gpl-3.0.html";

    #endregion
}