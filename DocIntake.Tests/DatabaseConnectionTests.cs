using DocIntake.Api.Configuration;
using Microsoft.Extensions.Configuration;

namespace DocIntake.Tests;

public class DatabaseConnectionTests
{
    private static IConfiguration Config(params (string Key, string Value)[] values) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(values.ToDictionary(v => v.Key, v => (string?)v.Value))
            .Build();

    [Fact]
    public void Missing_host_user_and_password_are_taken_from_the_PG_variables()
    {
        var config = Config(
            ("ConnectionStrings:Default", "Database=docintake;SSL Mode=Require"),
            ("PGHOST", "db.example.internal"),
            ("PGUSER", "dbadmin"),
            ("PGPASSWORD", "from-the-secret"));

        var resolved = DatabaseConnection.Resolve(config);

        Assert.Contains("Host=db.example.internal", resolved);
        Assert.Contains("Username=dbadmin", resolved);
        Assert.Contains("Password=from-the-secret", resolved);
        Assert.Contains("Database=docintake", resolved);
    }

    [Fact]
    public void Values_already_in_the_connection_string_are_not_overridden()
    {
        var config = Config(
            ("ConnectionStrings:Default", "Host=db;Port=5432;Database=docintake;Username=docintake;Password=dev-only"),
            ("PGHOST", "other-host"),
            ("PGUSER", "other-user"),
            ("PGPASSWORD", "other-password"));

        var resolved = DatabaseConnection.Resolve(config);

        Assert.Contains("Host=db", resolved);
        Assert.Contains("Username=docintake", resolved);
        Assert.Contains("Password=dev-only", resolved);
        Assert.DoesNotContain("other-", resolved);
    }

    [Fact]
    public void A_missing_connection_string_is_reported_clearly()
    {
        var ex = Assert.Throws<InvalidOperationException>(() => DatabaseConnection.Resolve(Config()));
        Assert.Contains("Default", ex.Message);
    }

    [Fact]
    public void The_description_never_contains_the_password()
    {
        var description = DatabaseConnection.Describe(
            "Host=db.example.internal;Port=5432;Database=docintake;Username=u;Password=super-secret");

        Assert.Equal("db.example.internal:5432/docintake", description);
        Assert.DoesNotContain("super-secret", description);
    }
}
