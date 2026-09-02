namespace AppSupportHub.IntegrationTests.Persistence;

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class PostgreSqlIntegrationCollectionDefinition
    : ICollectionFixture<PostgreSqlContainerFixture>
{
    public const string Name = "PostgreSQL integration";
}
