using AppSupportHub.Domain.Systems;
using AppSupportHub.Infrastructure.Persistence;
using AppSupportHub.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AppSupportHub.IntegrationTests.Persistence;

[Collection(PostgreSqlIntegrationCollectionDefinition.Name)]
public sealed class OptimisticConcurrencyTests(PostgreSqlContainerFixture fixture)
{
    [Fact]
    public async Task StaleApplicationSystemSaveThrowsConcurrencyExceptionAsync()
    {
        await fixture.ResetDatabaseAsync();
        ApplicationSystem applicationSystem = PostgreSqlTestData.CreateApplicationSystem();

        await using (AppSupportHubDbContext setupContext = fixture.CreateDbContext())
        {
            setupContext.Add(applicationSystem);
            await setupContext.SaveChangesAsync();
        }

        await using AppSupportHubDbContext firstContext = fixture.CreateDbContext();
        await using AppSupportHubDbContext staleContext = fixture.CreateDbContext();
        ApplicationSystem? first = await new ApplicationSystemRepository(firstContext).GetByIdAsync(
            applicationSystem.Id,
            CancellationToken.None);
        ApplicationSystem? stale = await new ApplicationSystemRepository(staleContext).GetByIdAsync(
            applicationSystem.Id,
            CancellationToken.None);
        Assert.NotNull(first);
        Assert.NotNull(stale);

        first.UpdateMetadata(
            first.Name,
            "First concurrent update.",
            first.Type,
            first.Criticality,
            first.BusinessOwner,
            first.TechnicalOwner,
            first.SupportTeam,
            first.VendorName,
            PostgreSqlTestData.CreatedAtUtc.AddHours(1));
        await firstContext.SaveChangesAsync();

        stale.UpdateMetadata(
            stale.Name,
            "Stale concurrent update.",
            stale.Type,
            stale.Criticality,
            stale.BusinessOwner,
            stale.TechnicalOwner,
            stale.SupportTeam,
            stale.VendorName,
            PostgreSqlTestData.CreatedAtUtc.AddHours(2));

        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => staleContext.SaveChangesAsync());
    }
}
