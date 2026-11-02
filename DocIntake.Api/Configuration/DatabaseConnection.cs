using Npgsql;

namespace DocIntake.Api.Configuration;

/// <summary>
/// Builds the PostgreSQL connection string from configuration.
///
/// The configured string ("ConnectionStrings:Default") may leave out the host, user and password.
/// Those are then taken from the standard PostgreSQL variables PGHOST, PGUSER and PGPASSWORD.
/// On AWS, ECS fills PGUSER and PGPASSWORD from Secrets Manager when the container starts,
/// so the password never appears in a task definition, an image or a file.
/// A string that already contains a value always wins, so local and compose setups are unchanged.
/// </summary>
public static class DatabaseConnection
{
    public static string Resolve(IConfiguration configuration)
    {
        var configured = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("Connection string 'Default' is not configured.");

        var builder = new NpgsqlConnectionStringBuilder(configured);

        if (string.IsNullOrWhiteSpace(builder.Host))
            builder.Host = configuration["PGHOST"];

        if (string.IsNullOrWhiteSpace(builder.Username))
            builder.Username = configuration["PGUSER"];

        if (string.IsNullOrWhiteSpace(builder.Password))
            builder.Password = configuration["PGPASSWORD"];

        return builder.ConnectionString;
    }

    /// <summary>The parts that are safe to log. Never includes the password.</summary>
    public static string Describe(string connectionString)
    {
        var b = new NpgsqlConnectionStringBuilder(connectionString);
        return $"{b.Host}:{b.Port}/{b.Database}";
    }
}
