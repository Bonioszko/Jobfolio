using App.Infrastructure;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace App.Tests;

public sealed class DatabaseConnectionStringTests
{
    [Fact]
    public void Explicit_connection_string_takes_precedence()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["ConnectionStrings:Postgres"] = "Host=explicit;Database=jobs"
        });

        Assert.Equal("Host=explicit;Database=jobs", DatabaseConnectionString.Create(configuration));
    }

    [Fact]
    public void Cloud_database_settings_build_a_bounded_private_connection_string()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["Database:Host"] = "10.42.0.2",
            ["Database:Password"] = "secret",
            ["Database:MaximumPoolSize"] = "5"
        });

        var result = new NpgsqlConnectionStringBuilder(DatabaseConnectionString.Create(configuration));

        Assert.Equal("10.42.0.2", result.Host);
        Assert.Equal(5, result.MaxPoolSize);
        Assert.Equal(SslMode.Disable, result.SslMode);
        Assert.Equal("secret", result.Password);
    }

    [Fact]
    public void Cloud_database_host_overrides_the_bundled_local_connection_string()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["ConnectionStrings:Postgres"] = "Host=localhost;Database=local",
            ["Database:Host"] = "10.42.0.2",
            ["Database:Password"] = "secret"
        });

        var result = new NpgsqlConnectionStringBuilder(DatabaseConnectionString.Create(configuration));

        Assert.Equal("10.42.0.2", result.Host);
        Assert.Equal("jobparser", result.Database);
    }

    [Fact]
    public void Cloud_database_settings_require_a_password()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["Database:Host"] = "10.42.0.2"
        });

        Assert.Throws<InvalidOperationException>(() => DatabaseConnectionString.Create(configuration));
    }

    private static IConfiguration BuildConfiguration(Dictionary<string, string?> values) =>
        new ConfigurationBuilder().AddInMemoryCollection(values).Build();
}
