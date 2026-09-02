using System.Text;
using AppSupportHub.Application.Abstractions.Results;
using AppSupportHub.Application.LegacyImports;
using AppSupportHub.Application.Systems.Inputs;
using AppSupportHub.Domain.ChangeAssessments;
using AppSupportHub.Domain.Systems;
using AppSupportHub.Domain.WorkItems;
using AppSupportHub.Infrastructure.LegacyImports;
using AppSupportHub.Infrastructure.Persistence;
using AppSupportHub.Infrastructure.Persistence.Repositories;
using AppSupportHub.IntegrationTests.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AppSupportHub.IntegrationTests.Phase05;

[Collection(PostgreSqlIntegrationCollectionDefinition.Name)]
public sealed class Phase05PersistenceAndCsvTests(PostgreSqlContainerFixture fixture)
{
    private const string Header =
        "LegacyId,Name,Description,Type,Criticality,LifecycleStatus,"
        + "BusinessOwner,TechnicalOwner,SupportTeam,VendorName\n";

    [Fact]
    public async Task AssessmentRoundTripUpdatesAndEnforcesOnePerWorkItemAsync()
    {
        await fixture.ResetDatabaseAsync();
        ApplicationSystem system = PostgreSqlTestData.CreateApplicationSystem();
        WorkItem workItem = PostgreSqlTestData.CreateWorkItem(
            system.Id,
            WorkItemType.ChangeRequest);
        ChangeAssessment assessment = CreateAssessment(workItem.Id);

        await using (AppSupportHubDbContext write = fixture.CreateDbContext())
        {
            await new ApplicationSystemRepository(write).AddAsync(system, CancellationToken.None);
            await new WorkItemRepository(write).AddAsync(workItem, CancellationToken.None);
            await new ChangeAssessmentRepository(write).AddAsync(assessment, CancellationToken.None);
            await write.SaveChangesAsync();
        }

        await using (AppSupportHubDbContext update = fixture.CreateDbContext())
        {
            var repository = new ChangeAssessmentRepository(update);
            ChangeAssessment actual = Assert.IsType<ChangeAssessment>(
                await repository.GetByWorkItemIdAsync(workItem.Id, CancellationToken.None));
            Assert.True(actual.Update(
                "Revised need", "Technical", "Security", ChangeRisk.High,
                "Acceptance", "Test", "Rollback", "demo.actor",
                PostgreSqlTestData.CreatedAtUtc.AddHours(1)));
            await update.SaveChangesAsync();
        }

        await using AppSupportHubDbContext verify = fixture.CreateDbContext();
        ChangeAssessment persisted = Assert.IsType<ChangeAssessment>(
            await new ChangeAssessmentRepository(verify).GetByWorkItemIdAsync(
                workItem.Id,
                CancellationToken.None));
        Assert.Equal("Revised need", persisted.BusinessNeed);
        Assert.Equal(ChangeRisk.High, persisted.Risk);
        await new ChangeAssessmentRepository(verify).AddAsync(
            CreateAssessment(workItem.Id),
            CancellationToken.None);
        await Assert.ThrowsAsync<DbUpdateException>(() => verify.SaveChangesAsync());
    }

    [Fact]
    public async Task ValidCsvPreviewFindsReadyFileAndDatabaseDuplicatesWithoutMutationAsync()
    {
        await fixture.ResetDatabaseAsync();
        ApplicationSystem existing = PostgreSqlTestData.CreateApplicationSystem("Existing Demo");
        await using AppSupportHubDbContext context = fixture.CreateDbContext();
        var repository = new ApplicationSystemRepository(context);
        await repository.AddAsync(existing, CancellationToken.None);
        await context.SaveChangesAsync();
        int before = await context.Set<ApplicationSystem>().CountAsync();
        string csv = Header
            + Row("L-1", "Ready Demo")
            + Row("L-2", "File Duplicate")
            + Row("L-2", "file duplicate")
            + Row("L-4", "existing demo");
        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));
        var handler = new PreviewLegacyCsvHandler(
            new CsvHelperLegacyCsvParser(),
            new ApplicationSystemInputFactory(),
            repository);

        ApplicationResult<LegacyCsvPreview> result = await handler.ExecuteAsync(
            new PreviewLegacyCsvCommand(stream, stream.Length, ".csv"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(LegacyCsvDisposition.Ready, result.Value.Rows[0].Disposition);
        Assert.True(result.Value.Rows[1].DuplicateLegacyIdInFile);
        Assert.True(result.Value.Rows[1].DuplicateNameInFile);
        Assert.True(result.Value.Rows[3].DuplicateNameInDatabase);
        Assert.Equal(1, result.Value.ReadyCount);
        Assert.Equal(3, result.Value.ReviewDuplicateCount);
        Assert.Equal(before, await context.Set<ApplicationSystem>().CountAsync());
        Assert.DoesNotContain(
            context.ChangeTracker.Entries(),
            entry => entry.State != EntityState.Unchanged);
    }

    [Theory]
    [InlineData("all")]
    public async Task InvalidCsvCasesReturnOneSafeValidationFailureAsync(string caseSet)
    {
        Assert.Equal("all", caseSet);
        await fixture.ResetDatabaseAsync();
        await using AppSupportHubDbContext context = fixture.CreateDbContext();
        var handler = new PreviewLegacyCsvHandler(
            new CsvHelperLegacyCsvParser(),
            new ApplicationSystemInputFactory(),
            new ApplicationSystemRepository(context));

        foreach ((byte[] content, long reportedLength, string expectedText) in InvalidCsvCases())
        {
            await using var stream = new MemoryStream(content);
            ApplicationResult<LegacyCsvPreview> result = await handler.ExecuteAsync(
                new PreviewLegacyCsvCommand(stream, reportedLength, ".csv"),
                CancellationToken.None);

            Assert.False(result.IsSuccess);
            Assert.Equal("validation.invalid_input", result.Error!.Code);
            Assert.Contains(
                expectedText,
                result.Error.Description,
                StringComparison.OrdinalIgnoreCase);
        }
    }

    private static IReadOnlyList<(byte[] Content, long ReportedLength, string ExpectedText)>
        InvalidCsvCases()
    {
        byte[] wrongHeader = Encoding.UTF8.GetBytes("Wrong,Header\nvalue,value\n");
        byte[] malformed = Encoding.UTF8.GetBytes(Header + "\"unterminated");
        byte[] small = Encoding.UTF8.GetBytes(Header + Row("L-1", "Size Demo"));
        byte[] invalidUtf8 = [0xff, 0xfe, 0xfd];
        string tooManyRows = Header + string.Concat(
            Enumerable.Range(1, 101).Select(index => Row($"L-{index}", $"Demo {index}")));
        return
        [
            (wrongHeader, wrongHeader.Length, "header"),
            (malformed, malformed.Length, "malformed"),
            (small, PreviewLegacyCsvHandler.MaximumFileSize + 1, "256"),
            (invalidUtf8, invalidUtf8.Length, "UTF-8"),
            (Encoding.UTF8.GetBytes(tooManyRows), Encoding.UTF8.GetByteCount(tooManyRows), "100"),
        ];
    }

    private static ChangeAssessment CreateAssessment(Guid workItemId) => ChangeAssessment.Create(
        workItemId, "Need", "Technical", "Security", ChangeRisk.Medium,
        "Acceptance", "Test", "Rollback", "demo.actor", PostgreSqlTestData.CreatedAtUtc);

    private static string Row(string legacyId, string name) =>
        $"{legacyId},{name},Fictional preview,Custom,Medium,Active,"
        + "Business,Technical,Support,\n";
}
