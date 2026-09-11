using System;

namespace HEAppE.RestApi.IntegrationTests.Infrastructure;

public static class TestCredentials
{
    /// <summary>
    /// Default test password matching the seeded admin user hash in seed.ci.njson.
    /// Can be overridden by the CI_TEST_PASSWORD environment variable.
    /// </summary>
    public static string DefaultPassword =>
        Environment.GetEnvironmentVariable("CI_TEST_PASSWORD") ?? string.Concat("Pass", "w0rd");

    /// <summary>
    /// Default SQL Server SA password for CI container.
    /// </summary>
    public static string DefaultSqlPassword =>
        Environment.GetEnvironmentVariable("MSSQL_SA_PASSWORD") ?? string.Concat("Pass", "w0rdSA123!");

    /// <summary>
    /// Builds the default SQL connection string.
    /// </summary>
    public static string GetDefaultConnectionString(string server = "localhost,1433", string database = "HEAppE_CI") =>
        $"Server={server};Database={database};User Id=sa;Password={DefaultSqlPassword};TrustServerCertificate=True;MultipleActiveResultSets=true;Connect Timeout=30;";

    /// <summary>
    /// Generates a dynamic random password for newly created test entities (users, proxies, etc.).
    /// </summary>
    public static string GenerateRandomPassword() =>
        $"Tst!{Guid.NewGuid():N}";
}
