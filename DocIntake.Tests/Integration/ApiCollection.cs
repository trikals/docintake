namespace DocIntake.Tests.Integration;

/// <summary>
/// All integration tests share one API instance and one database container.
/// Starting the app and Postgres takes seconds; doing it per test would be slow.
/// </summary>
[CollectionDefinition(Name)]
public class ApiCollection : ICollectionFixture<ApiFactory>
{
    public const string Name = "Api";
}
