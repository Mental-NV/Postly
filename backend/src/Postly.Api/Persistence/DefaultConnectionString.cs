using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;

namespace Postly.Api.Persistence;

public static class DefaultConnectionString
{
    private const string DefaultDatabaseFileName = "postly.db";

    public static string Resolve(IConfiguration configuration, bool isDevelopment)
    {
        var configuredConnectionString = configuration.GetConnectionString("DefaultConnection");

        if (!string.IsNullOrWhiteSpace(configuredConnectionString))
        {
            EnsureDataDirectoryExists(configuredConnectionString);
            return configuredConnectionString;
        }

        var fallbackConnectionString = BuildFallbackConnectionString(isDevelopment);
        EnsureDataDirectoryExists(fallbackConnectionString);

        return fallbackConnectionString;
    }

    private static string BuildFallbackConnectionString(bool isDevelopment)
    {
        var homeDirectory = Environment.GetEnvironmentVariable("HOME");

        if (!isDevelopment && !string.IsNullOrWhiteSpace(homeDirectory))
        {
            var persistentDatabasePath = Path.Combine(homeDirectory, "data", DefaultDatabaseFileName);
            return BuildSqliteConnectionString(persistentDatabasePath);
        }

        return BuildSqliteConnectionString(DefaultDatabaseFileName);
    }

    private static string BuildSqliteConnectionString(string dataSource)
    {
        var connectionStringBuilder = new SqliteConnectionStringBuilder
        {
            DataSource = dataSource
        };

        return connectionStringBuilder.ToString();
    }

    private static void EnsureDataDirectoryExists(string connectionString)
    {
        var dataSource = new SqliteConnectionStringBuilder(connectionString).DataSource;

        if (string.IsNullOrWhiteSpace(dataSource)
            || string.Equals(dataSource, ":memory:", StringComparison.OrdinalIgnoreCase)
            || dataSource.StartsWith("file:", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var directoryPath = Path.GetDirectoryName(dataSource);

        if (string.IsNullOrWhiteSpace(directoryPath))
        {
            return;
        }

        Directory.CreateDirectory(directoryPath);
    }
}
