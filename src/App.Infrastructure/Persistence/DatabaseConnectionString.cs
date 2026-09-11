using Microsoft.Extensions.Configuration;
using Npgsql;

namespace App.Infrastructure;

internal static class DatabaseConnectionString
{
    private const string LocalDefault =
        "Host=localhost;Port=5432;Database=jobparser;Username=jobparser;Password=jobparser";

    public static string Create(IConfiguration configuration)
    {
        var host = configuration["Database:Host"]?.Trim();
        if (string.IsNullOrWhiteSpace(host))
        {
            return configuration.GetConnectionString("Postgres")?.Trim() is { Length: > 0 } connectionString
                ? connectionString
                : LocalDefault;
        }

        var password = configuration["Database:Password"];
        if (string.IsNullOrEmpty(password))
        {
            throw new InvalidOperationException(
                "Database:Password is required when Database:Host is configured.");
        }

        var maximumPoolSize = ConfigurationValues.GetPositiveInt(
            configuration,
            "Database:MaximumPoolSize",
            5);
        if (maximumPoolSize > 50)
        {
            throw new InvalidOperationException("Database:MaximumPoolSize cannot exceed 50.");
        }

        return new NpgsqlConnectionStringBuilder
        {
            Host = host,
            Port = ConfigurationValues.GetPositiveInt(configuration, "Database:Port", 5432),
            Database = configuration["Database:Name"]?.Trim() ?? "jobparser",
            Username = configuration["Database:Username"]?.Trim() ?? "jobparser",
            Password = password,
            MaxPoolSize = maximumPoolSize,
            Timeout = 15,
            CommandTimeout = 30,
            KeepAlive = 30,
            SslMode = SslMode.Disable
        }.ConnectionString;
    }
}
